using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class matchLogic : MonoBehaviour
{
    [Header("showing players")]
    public GameObject playerUI;
    public Transform playersParent;
    public Sprite[] playerSprits;
    public void afterStart(Lobby joinedLobby)
    {
        foreach(Player player in joinedLobby.Players)
        {
            GameObject uiInstance = Instantiate(playerUI, playersParent);

            uiInstance.transform.GetChild(0).GetComponent<Image>().sprite = playerSprits[int.Parse(player.Data["playerCharacter"].Value)];
            uiInstance.transform.GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>().text = player.Data["playerName"].Value;

            Debug.Log(int.Parse(player.Data["playerCharacter"].Value));
        }
    }
}
