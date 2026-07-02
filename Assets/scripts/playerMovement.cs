using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Hierarchy;
using UnityEditor.ShaderGraph;

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
    public float horiznotalCheckLength;

    [Header("sliding on slopes")]
    public float unClimable;
    public float slideAcc;
    public float slideFalloff;
    Vector2 lastTangent;
    float slideAmount;

    [Header("animation")]
    public Animator anim;
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

        grounded(length * 1.4f, out RaycastHit2D hit, true);

        anim.SetBool("running", pointDir.x != 0 && hit);
        anim.SetBool("flying", !hit);

        bodySprite.transform.localScale = new Vector3(sizeX, 1, 1);
        if (sizeX != -moveDir.x)
        {
            sizeX = Mathf.Clamp(sizeX + (flipSpeed * -moveDir.x * Time.deltaTime), -1, 1);
        }

        transform.up = grounded(length * 1.5f) || hit ? hit.normal : rb.linearVelocity;
    }
    private void FixedUpdate()
    {
        if (IsOwner == false || IsSpawned == false) return;

        float moveValue = moveDir.x * speed * speedMultiplier;

        RaycastHit2D verHit = Physics2D.Raycast(transform.position, -transform.up, length, groundMask);
        RaycastHit2D forwardHit = Physics2D.Raycast(transform.position + length * -transform.up, new Vector2(moveDir.x, 0), horiznotalCheckLength, groundMask);
        RaycastHit2D backHit = Physics2D.Raycast(transform.position + length * -transform.up, new Vector2(-moveDir.x, 0), horiznotalCheckLength, groundMask);

        if (verHit || backHit || forwardHit)
        {
            gPull = 0;

            RaycastHit2D angleHit = verHit ? verHit : (forwardHit ? forwardHit : backHit);

            //float error = Vector3.Dot((Vector2)transform.position - (length * (Vector2)transform.up) - angleHit.point, angleHit.normal);
            //if (error > 0)
            //{
            //    rb.position -= angleHit.normal * error;
            //}

            float angle = Mathf.Atan2(angleHit.normal.x, angleHit.normal.y) * Mathf.Rad2Deg;
            if (Mathf.Abs(angle) > unClimable)
            {
                slideAmount = angle > 0 ? Mathf.Max(slideAmount - slideAcc, -gravityClamp) : Mathf.Min(slideAmount + slideAcc, gravityClamp);
            }

            Vector2 tangent = new Vector2(-angleHit.normal.y, angleHit.normal.x);
            rb.linearVelocity = (Vector2)(transform.right * moveValue) + (tangent * slideAmount) + knockBackinfo.targetVel;
            lastTangent = tangent;
        }
        else
        {
            gPull = Mathf.Max(gPull - gravityStrength, -gravityClamp);
            rb.linearVelocity = new Vector2(moveValue, gPull) + (lastTangent * slideAmount) + knockBackinfo.targetVel;
        }
        slideAmount = slideAmount < 0 ? Mathf.Min(slideAmount + slideFalloff, 0) : Mathf.Max(slideAmount - slideFalloff, 0);
    }

    bool grounded(float length, out RaycastHit2D hit, bool useLocalDown = false)
    {
        return hit = Physics2D.Raycast(transform.position, useLocalDown ? -transform.up : Vector2.down, length, groundMask);
    }
    bool grounded(float length, bool useLocalDown = false)
    {
        return Physics2D.Raycast(transform.position, useLocalDown ? -transform.up : Vector2.down, length, groundMask);
    }
}