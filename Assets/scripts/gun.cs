using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class gun : NetworkBehaviour
{
    [Header("Input")]
    public InputActionReference shooting;
    public InputActionReference aiming;
    Vector2 mousePos;

    [Header("not spamable")]
    public float fireRate;
    float timer;

    [Header("shooting")]
    public items heldItem;
    public enum items { shotgun, grappleHook, rocketLauncher}

    public rocket rocket;
    public grappleHook grappleHook;
    grappleHook spawnedHook;

    [Header("self knockBack")]
    public playerMovement movement;
    public knockable knockBackinfo;
    public Collider2D col;
    public float knockBackStrength;

    [Header("enemy knockBack")]
    public float gunStrength;
    public float shootSize;
    public LayerMask playersMask;

    [Header("placing the gun")]
    public Transform gunObject;
    public float holdingCircleRadius;

    [Header("placing the arms on the gun")]
    public Transform armsHint;
    public Vector3 defulteHintPos;

    public bool oneHandedGun;
    public Transform targetR, targetL;
    Vector2 rPos, lPos;

    private void OnEnable()
    {
        shooting.action.started += shoot;
    }
    private void OnDisable()
    {
        shooting.action.started -= shoot;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner == false || IsSpawned == false) return;

        rPos = targetR.transform.localPosition;
        lPos = targetL.transform.localPosition;
    }

    void Update()
    {
        if (IsOwner == false || IsSpawned == false) return;

        timer += Time.deltaTime;

        mousePos = Camera.main.ScreenToWorldPoint(aiming.action.ReadValue<Vector2>());

        gunObject.position = transform.position + ((Vector3)mousePos - transform.position).normalized * holdingCircleRadius;
        gunObject.right = ((Vector3)mousePos - transform.position).normalized;

        bool lookingRight = Vector3.Dot(gunObject.right, Vector3.right) > 0;

        if (oneHandedGun == false)
        {
            armsHint.position = transform.position + (lookingRight ? defulteHintPos : new Vector3(-defulteHintPos.x, defulteHintPos.y));

            targetL.localPosition = lookingRight ? lPos : rPos;
            targetR.localPosition = lookingRight ? rPos : lPos;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (IsOwner == false || IsSpawned == false) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(mousePos, 0.5f);

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(mousePos, ((Vector2)transform.position - mousePos).normalized * knockBackStrength);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position + ((Vector3)mousePos - transform.position).normalized * (shootSize + holdingCircleRadius), shootSize);
    }

    void shoot(InputAction.CallbackContext cntx)
    {
        if (IsOwner == false || IsSpawned == false) return;

        if (timer > fireRate)
        {
            timer = 0;

            switch (heldItem)
            {
                case items.shotgun:

                    knockBackinfo.KnockBack(((Vector2)transform.position - mousePos).normalized * knockBackStrength, movement);
                    shootServerRpc(transform.position + ((Vector3)mousePos - transform.position).normalized * (shootSize + holdingCircleRadius), false);

                    break;
                case items.grappleHook:

                    shootGrappleHookServerRpc(gunObject.transform.position, gunObject.rotation);

                    break;
                case items.rocketLauncher:

                    shootRocketServerRpc(gunObject.position, gunObject.rotation);

                    break;
                default:
                    break;
            }
        }
    }

    [ServerRpc]
    void shootGrappleHookServerRpc(Vector3 spawnPos, Quaternion spawnRot)
    {
        if (spawnedHook != null) return;
        spawnedHook = Instantiate(grappleHook, spawnPos, spawnRot);

        spawnedHook.Initialize(GetComponent<NetworkObject>().NetworkObjectId);
        spawnedHook.GetComponent<NetworkObject>().Spawn();
    }

    [ServerRpc]
    void shootRocketServerRpc(Vector3 spawnPos, Quaternion spawnRot)
    {
        rocket newRocket = Instantiate(rocket, spawnPos, spawnRot);
        newRocket.GetComponent<NetworkObject>().Spawn();
        newRocket.spawner = this;
    }

    [ServerRpc]
    public void shootServerRpc(Vector3 shooterPos, bool dontIgnoreSelf)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(shooterPos, shootSize, playersMask);

        foreach (Collider2D hit in hits)
        {
            if (hit != col || dontIgnoreSelf)
            {
                Vector2 knockDir = ((Vector2)(hit.transform.position - shooterPos)).normalized * gunStrength;
                ulong hitClientId = hit.GetComponent<NetworkObject>().OwnerClientId;

                applyKnockbackClientRpc(hit.GetComponent<NetworkObject>().NetworkObjectId, knockDir);
            }
        }
    }
    [ClientRpc]
    public void applyKnockbackClientRpc(ulong networkObjectId, Vector2 knockDir)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
        {
            netObj.GetComponent<knockable>().KnockBack(knockDir, netObj.GetComponent<playerMovement>());
        }
    }
}
