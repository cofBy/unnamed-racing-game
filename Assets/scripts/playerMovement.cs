using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using JetBrains.Annotations;

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

    [Header("gravity")]
    public float gravityStrength;
    public float gPull;

    public float gravityClamp;

    [Header("knockBack")]
    public knockable knockBackinfo;

    [Header("check if grounded")]
    public LayerMask groundMask;
    public float length;

    [Header("animation")]
    public Animator anim;
    public float runningThreshold;
    public float flyingThreshold;

    public GameObject bodySprite;
    public float flipSpeed;
    float sizeX;
    private void Start()
    {
        name = "player " + OwnerClientId;

        pointDir = Vector2.right;
        moveDir = Vector2.right;
        sizeX = -1;
    }
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


        bool flying = !grounded(length * 1.25f);
        bool running = Mathf.Abs(pointDir.x) > runningThreshold && grounded(length);
        anim.SetBool("running", running);
        anim.SetBool("flying", flying);

        bodySprite.transform.localScale = new Vector3(sizeX, 1, 1);
        if (sizeX != -moveDir.x)
        {
            sizeX = Mathf.Clamp(sizeX + (flipSpeed * -moveDir.x * Time.deltaTime), -1, 1);
        }

        float angle = Mathf.Atan2(-rb.linearVelocityX, rb.linearVelocityY) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, flying ? angle : 0);
    }
    private void FixedUpdate()
    {

        if (grounded(length))
        {
            gPull = 0;
        }
        else
        {
            gPull = Mathf.Max(gPull - gravityStrength, -gravityClamp);
        }

        rb.linearVelocity = new Vector2(moveDir.x * speed * speedMultiplier, gPull) + knockBackinfo.targetVel;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, Vector2.down * length);
    }

    bool grounded(float length)
    {
        return Physics2D.Raycast(transform.position, Vector2.down, length, groundMask);
    }
}