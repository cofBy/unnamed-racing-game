using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class lobby : MonoBehaviour
{
    [Header("choosing name")]
    public TMP_InputField nameInput;
    public GameObject createJoinPanel;
    public GameObject namePanel;
    public Button doneChoosing;

    [Header("making lobbys UI")]
    public Button makeLobby;
    public TMP_InputField lobbyName;
    public Toggle isPublic;

    [Header("Joining lobbies")]
    public Button joinButton;
    public TMP_InputField codeInput;

    public Button quickJoinButton;

    [Header("searching for lobbys UI")]
    public float timeBetweenRefreshes;
    public TMP_InputField searchBar;
    bool isReady = false;
    bool isQuerying = false;
    float searchCooldown = 0f;

    [Header("showing existing lobbies")]
    public Button lobbyObject;
    public GameObject lobbiesParent;

    List<Button> spawnedLobbies = new List<Button>();

    [Header("heartBeeting")]
    Lobby hostLobby;
    Lobby joinedLobby;

    float beatingTimer;
    float pollTimer;

    [Header("leaving the lobby")]
    public Button leaveButton;
    string lastPlayerSnapshot;

    [Header("displaying the lobby")]
    public GameObject lobbyPanel;
    public TMP_Dropdown mapSelection;
    public TextMeshProUGUI selectedMap;

    public GameObject playerCard;
    List<GameObject> allPlayerCards = new List<GameObject>();

    public TextMeshProUGUI lobbyNameDisplay;

    public Transform playerCardsParent;

    int lastMap = -1;
    bool isPolling = false;

    [Header("start game")]
    public Button playButton;
    public GameObject lobbyLogicParent;
    public string startGameKey;

    private void Awake()
    {
        createJoinPanel.SetActive(false);
        namePanel.SetActive(true);

        makeLobby.onClick.AddListener(creatLobby);
        joinButton.onClick.AddListener(() => joinLobby(codeInput.text, false));
        quickJoinButton.onClick.AddListener(quickJoin);
        leaveButton.onClick.AddListener(() => leaveLobby(AuthenticationService.Instance.PlayerId));
        doneChoosing.onClick.AddListener(chooseName);
        playButton.onClick.AddListener(startGame);

        beatingTimer = 0;
        pollTimer = 0;
    }

    async void Start()
    {
        InitializationOptions options = new InitializationOptions();

#if UNITY_EDITOR
        options.SetProfile("EditorPlayer");
#else
    // Standalone builds get a random profile name so multiple builds can run at once
    options.SetProfile($"BuildPlayer_{Random.Range(0, 9999)}");
#endif
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        isReady = true;
        await listLobbies();
    }

    void Update()
    {
        sendHeartBeat();
        sendPoll();
        displayLobby();

        if (joinedLobby == null)
        {
            searchCooldown -= Time.deltaTime;
            if (searchCooldown < 0)
            {
                _ = listLobbies();
                searchCooldown = timeBetweenRefreshes;
            }
        }
    }

    void chooseName()
    {
        if (string.IsNullOrWhiteSpace(nameInput.text) == false)
        {
            namePanel.SetActive(false);
            createJoinPanel.SetActive(true);
        }
    }

    async void startGame()
    {
        try
        {
            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(3);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
            RelayServerData serverData = alloc.ToRelayServerData("udp");

            if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is UnityTransport transport)
            {
                transport.SetRelayServerData(serverData);
            }
            else
            {
                Debug.LogError("UnityTransport component not found on NetworkManager Config!");
                return;
            }

            NetworkManager.Singleton.StartHost();
            lobbyLogicParent.SetActive(false);

            UpdateLobbyOptions options = new UpdateLobbyOptions { Data = new Dictionary<string, DataObject> { { startGameKey, new DataObject(DataObject.VisibilityOptions.Member, joinCode) } } };
            Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, options);
            joinedLobby = lobby;
        }
        catch (RelayServiceException exc)
        {
            Debug.Log(exc);
        }
    }

    async void sendHeartBeat()
    {
        if (hostLobby != null)
        {
            beatingTimer -= Time.deltaTime;

            if (beatingTimer < 0)
            {
                beatingTimer = 3f;
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }
    async void sendPoll()
    {
        if (joinedLobby == null || isPolling) return;

        pollTimer -= Time.deltaTime;
        if (pollTimer < 0)
        {
            pollTimer = 1f;
            isPolling = true;
            try
            {
                Lobby l = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                joinedLobby = l;

                string localId = AuthenticationService.Instance.PlayerId;
                if (joinedLobby.Players.Any(p => p.Id == localId) == false)
                {
                    joinedLobby = null;
                    hostLobby = null;
                    lastMap = -1;
                }

                if (joinedLobby.Data != null && joinedLobby.Data.TryGetValue(startGameKey, out DataObject startGameData) && startGameData.Value != "0")
                {
                    if (isHost() == false)
                    {
                        joinRelay(joinedLobby.Data[startGameKey].Value);
                    }
                    joinedLobby = null;
                }
            }
            catch (LobbyServiceException exc)
            {
                Debug.LogWarning(exc);
            }
            finally
            {
                isPolling = false;
            }
        }
    }

    async void joinRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);
            RelayServerData serverData = joinAlloc.ToRelayServerData("udp");

            if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is UnityTransport transport)
            {
                transport.SetRelayServerData(serverData);
            }
            else
            {
                Debug.LogError("UnityTransport component not found on NetworkManager Config!");
                return;
            }

            NetworkManager.Singleton.StartClient();

            lobbyLogicParent.SetActive(false);
        }
        catch (RelayServiceException exc)
        {
            Debug.Log(exc);
        }
    }

    void displayLobby()
    {
        lobbyPanel.SetActive(joinedLobby != null);

        if (joinedLobby == null) return;


        mapSelection.gameObject.SetActive(isHost());
        selectedMap.gameObject.SetActive(!isHost());

        playButton.gameObject.SetActive(isHost());

        if (isHost())
        {

            if (mapSelection.value != lastMap)
            {
                lastMap = mapSelection.value;
                updateLobby("map", mapSelection.captionText.text, DataObject.IndexOptions.S1);
            }
        }
        else
        {
            mapSelection.gameObject.SetActive(false);
            selectedMap.gameObject.SetActive(true);

            if (joinedLobby.Data != null && joinedLobby.Data.ContainsKey("map"))
            {
                selectedMap.text = "selected map : " + joinedLobby.Data["map"].Value;
            }
        }
        string snapshot = string.Join(",", joinedLobby.Players.ConvertAll(p => p.Id));

        if (snapshot != lastPlayerSnapshot)
        {
            lastPlayerSnapshot = snapshot;

            foreach (GameObject card in allPlayerCards)
                Destroy(card);
            allPlayerCards.Clear();

            foreach (Player player in joinedLobby.Players)
            {
                GameObject spawnedCard = Instantiate(playerCard, playerCardsParent);
                allPlayerCards.Add(spawnedCard);

                spawnedCard.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = player.Data["playerName"].Value;

                Button playerKickButton = spawnedCard.transform.GetChild(1).GetComponent<Button>();
                string capturedId = player.Id;
                playerKickButton.onClick.AddListener(() => leaveLobby(capturedId));
                playerKickButton.gameObject.SetActive(isHost() && player.Id != AuthenticationService.Instance.PlayerId);
            }
        }


        lobbyNameDisplay.text = joinedLobby.Name;

    }

    bool isHost()
    {
        return joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    async void updateLobby(string key, string value, DataObject.IndexOptions index = 0)
    {
        if (hostLobby == null) return;

        try
        {
            UpdateLobbyOptions options = new UpdateLobbyOptions();
            options.Data = new Dictionary<string, DataObject> { { key, new DataObject(DataObject.VisibilityOptions.Public, value, index) } };

            hostLobby = await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, options);
        }
        catch (LobbyServiceException exc)
        {
            Debug.Log(exc);
        }
    }

    async void creatLobby()
    {
        try
        {
            if (lobbyName.text.Length != 0)
            {
                Player firstPlayer = getPlayer();
                CreateLobbyOptions options = new CreateLobbyOptions { IsPrivate = !isPublic.isOn, Player = firstPlayer };

                Lobby newLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName.text, 4, options);
                hostLobby = newLobby;
                joinedLobby = hostLobby;

                Debug.Log($"lobby's code: {newLobby.LobbyCode}");
            }
        }
        catch (LobbyServiceException exc)
        {
            Debug.LogWarning(exc);
        }
    }

    async Task listLobbies()
    {
        if (!isReady || isQuerying) return;
        isQuerying = true;

        try
        {
            List<QueryFilter> filters = new List<QueryFilter>();
            if (searchBar.text != "")
            {
                filters.Add(new QueryFilter(QueryFilter.FieldOptions.Name, searchBar.text, QueryFilter.OpOptions.CONTAINS));
            }

            QueryLobbiesOptions qlOptions = new QueryLobbiesOptions { Filters = filters };
            QueryResponse shownLobbies = await LobbyService.Instance.QueryLobbiesAsync(qlOptions);

            foreach (Button button in spawnedLobbies)
            {
                Destroy(button.gameObject);
            }
            spawnedLobbies.Clear();

            foreach (Lobby l in shownLobbies.Results)
            {
                Lobby captured = l;
                Button newLobby = Instantiate(lobbyObject, lobbiesParent.transform);

                spawnedLobbies.Add(newLobby);

                newLobby.onClick.AddListener(() => joinLobby(captured.Id, true));
                newLobby.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = captured.Name;
            }
        }
        catch (LobbyServiceException exc)
        {
            Debug.LogWarning(exc);
        }
        finally
        {
            isQuerying = false;
        }
    }

    Player getPlayer()
    {
        return new Player { Data = new Dictionary<string, PlayerDataObject> { { "playerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, nameInput.text) } } };
    }

    async void joinLobby(string code, bool useID)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return;
        }
        try
        {
            if (useID)
            {
                Lobby l = await LobbyService.Instance.JoinLobbyByIdAsync(code, new JoinLobbyByIdOptions { Player = getPlayer() });
                joinedLobby = l;
            }
            else
            {
                Lobby l = await LobbyService.Instance.JoinLobbyByCodeAsync(code, new JoinLobbyByCodeOptions { Player = getPlayer() });
                joinedLobby = l;
            }
        }
        catch (LobbyServiceException exc)
        {
            Debug.LogWarning(exc);
        }
    }
    async void quickJoin()
    {
        try
        {
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions { Player = getPlayer() };
            Lobby l = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            joinedLobby = l;
        }
        catch (LobbyServiceException exc)
        {
            Debug.LogWarning(exc);
        }
    }
    async void leaveLobby(string id)
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, id);
            if (id == AuthenticationService.Instance.PlayerId)
            {
                joinedLobby = null;
                hostLobby = null;
                lastMap = -1;
            }
        }
        catch (LobbyServiceException exc)
        {
            Debug.Log(exc);
        }
    }
}