using Unity.Netcode;
using UnityEngine;

public class portal : NetworkBehaviour
{
    [Header("detecing player touch")]
    public CapsuleCollider2D[] portals;
    public LayerMask playerMask;

    [Header("teleporting players")]
    public float distance;
    public float portalStrength;

    [Header("one way portals")]
    public bool isOneWay;
    public Material purblePortal;

    bool teleported;
    public float normalSize;
    public float maxDistance;
    public float closingSpeed;
    private void Update()
    {
        for (int i = 0; i < portals.Length; i++)
        {
            if (i == 1) continue;
            ColliderArray2D others = portals[i].GetContactColliders(new ContactFilter2D { layerMask = playerMask });

            foreach (Collider2D other in others)
            {
                playerMovement player = other.GetComponent<playerMovement>();
                CapsuleCollider2D gotoPortal = portals[(i + 1) % portals.Length];

                player.transform.position = gotoPortal.transform.position + (distance * gotoPortal.transform.right);

                player.gPull = 0;
                player.knockBackinfo.targetVel = gotoPortal.transform.right * player.knockBackinfo.targetVel.magnitude;
                teleported = true;
            }
        }
        if (isOneWay)
        {
            NetworkObject localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (localPlayer != null && teleported == false)
            {
                float sizeT = Mathf.InverseLerp(0, maxDistance, Vector2.Distance(portals[0].transform.position, localPlayer.transform.position));
                purblePortal.SetFloat("_size", Mathf.Lerp(normalSize, 1.2f, sizeT));
            }
            if (teleported)
            {
                purblePortal.SetFloat("_size", Mathf.MoveTowards(purblePortal.GetFloat("_size"), 1.2f, closingSpeed * Time.deltaTime));
                if (purblePortal.GetFloat("_size") > 0.9f)
                {
                    teleported = false;
                }
            }
        }
    }
}
