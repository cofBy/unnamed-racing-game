using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class playerMovement : NetworkBehaviour
{
    [Header("Input")]
    public InputActionReference moving;

    Vector2 moveDir;
    Vector2 pointDir;

    [Header("moving")]
    public Rigidbody2D rb;

    public float speedMultiplier;
    float speed;

    public float timeToMaxSpeed;
    float timer;
    bool setTimer;

    public AnimationCurve accCurve;

    [Header("KnockBack")]
    public Vector2 targetVel;
    private void OnEnable()
    {
        moving.action.Enable();
    }
    private void Update()
    {
        if (IsOwner == false || IsSpawned == false) return;

        pointDir = moving.action.ReadValue<Vector2>();

        if (pointDir.x != 0)
        {
            moveDir = moving.action.ReadValue<Vector2>();

            if (setTimer == false)
            {
                timer = 0;
                setTimer = true;
            }

            timer = Mathf.Min(timer + Time.deltaTime, timeToMaxSpeed);
        }
        else
        {
            setTimer = false;
            timer = Mathf.Max(timer - Time.deltaTime, 0);
        }

        float dt = timer / timeToMaxSpeed;
        speed = accCurve.Evaluate(dt);
    }
    private void FixedUpdate()
    {
        if (IsOwner == false || IsSpawned == false) return;

        rb.linearVelocity = new Vector2(moveDir.x * speed * speedMultiplier, rb.linearVelocityY) + targetVel;
    }
}
