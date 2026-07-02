using Unity.Netcode;
using UnityEngine;

public class cameraMovment : NetworkBehaviour
{
    [Header("getting the player")]
    public Rigidbody2D target;
    public gun playerGun;

    [Header("following the player")]
    public Vector3 offset;

    [Range(0, 1)] public float followBehavior;
    public float velocityCab;

    Vector3 vel;
    public float speed;

    [Header("zooming out")]
    public float minVel, maxVel;
    public float minSize, maxSize;
    public float sizeChangingSpeed;
    float sizeVel;

    [Header("followVars")]
    Vector3 lookAhead;
    Vector3 awayFromMouse;
    private void Update()
    {
        if (IsOwner == false || IsSpawned == false) return;

        lookAhead = target.transform.position + Vector3.ClampMagnitude(target.linearVelocity, velocityCab);
        awayFromMouse = target.transform.position + Vector3.ClampMagnitude(target.transform.position - Camera.main.ScreenToWorldPoint(playerGun.aiming.action.ReadValue<Vector2>()), velocityCab);

        Vector3 follow = Vector3.Lerp(lookAhead, awayFromMouse, followBehavior);
        Camera.main.transform.position = Vector3.SmoothDamp(Camera.main.transform.position - offset, follow, ref vel, speed) + offset;

        float size = Mathf.InverseLerp(minVel, maxVel, Mathf.Abs(target.linearVelocity.magnitude));
        Camera.main.orthographicSize = Mathf.SmoothDamp(Camera.main.orthographicSize, Mathf.Lerp(minSize, maxSize, size), ref sizeVel, sizeChangingSpeed);
    }
}
