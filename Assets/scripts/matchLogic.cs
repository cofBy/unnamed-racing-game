using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class matchLogic : NetworkBehaviour
{
    [Header("showing players")]
    public GameObject playerUI;
    public Transform playersParent;
    public Sprite[] playerSprits;

    [Header("winning")]
    public lobby lobbyLogic;
    bool winDeclared;

    [Header("removing UI")]
    List<GameObject> spawnedUI = new List<GameObject>();

    public void afterStart()
    {
        foreach(Player player in lobbyLogic.joinedLobby.Players)
        {
            GameObject uiInstance = Instantiate(playerUI, playersParent);
            spawnedUI.Add(uiInstance);

            uiInstance.transform.GetChild(0).GetComponent<Image>().sprite = playerSprits[int.Parse(player.Data["playerCharacter"].Value)];
            uiInstance.transform.GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>().text = player.Data["playerName"].Value;
        }

        winDeclared = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.gameObject.CompareTag("Player")) return;

        foreach (GameObject ui in spawnedUI)
        {
            Destroy(ui);
        }
        spawnedUI.Clear();

        if (!IsServer) return;
        if (winDeclared) return;

        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj == null) return;

        winDeclared = true;
        string winnerName = lobbyLogic.GetPlayerName(netObj.OwnerClientId);
        AnnounceWinnerClientRpc(netObj.OwnerClientId, winnerName);
    }

    [ClientRpc]
    void AnnounceWinnerClientRpc(ulong winnerClientId, string winnerName)
    {
        lobbyLogic.ReturnToLobby(winnerClientId, winnerName);
    }
}
