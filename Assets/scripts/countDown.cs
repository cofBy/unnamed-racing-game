using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class countDown : NetworkBehaviour
{
    [Header("detecting spawned players")]
    public int requiredPlayers;
    public int spawnedPlayerCount;

    [Header("counting down")]
    public float maxTime;
    NetworkVariable<float> timer = new NetworkVariable<float>(0f);

    [Header("stoping the players")]
    List<Rigidbody2D> rbs = new List<Rigidbody2D>();
    public NetworkVariable<bool> canMove = new NetworkVariable<bool>(false);

    [Header("displaying timer")]
    public TextMeshProUGUI timerUI;

    public static countDown Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        spawnedPlayerCount++;
        NetworkObject playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerObj != null)
        {
            rbs.Add(playerObj.GetComponent<Rigidbody2D>());
        }

        if (spawnedPlayerCount == requiredPlayers)
        {
            timer.Value = maxTime;
        }
    }

    private void Update()
    {
        if (IsServer && timer.Value > 0)
        {
            timer.Value = Mathf.Max(0, timer.Value - Time.deltaTime);
            if (timer.Value <= 0)
            {
                canMove.Value = true;
            }
        }

        timerUI.gameObject.SetActive(timer.Value - Time.deltaTime > 0);
        timerUI.text = Mathf.Round(timer.Value).ToString();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}
