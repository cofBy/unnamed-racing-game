using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class gun : NetworkBehaviour
{
    [Header("Input")]
    public InputActionReference shooting;

    [Header("not spamable")]
    public float fireRate;
    float timer;

    private void OnEnable()
    {
        shooting.action.started += shoot;
    }
    private void OnDisable()
    {
        shooting.action.started -= shoot;
    }

    void shoot(InputAction.CallbackContext cntx)
    {
        if (IsOwner == false || IsSpawned == false) return;

        Debug.Log("googoo gaagaa");
    }
}
