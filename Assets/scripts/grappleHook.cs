using UnityEngine;
using Unity.Netcode;

public class grappleHook : NetworkBehaviour
{
    [Header("Networking")]
    readonly NetworkVariable<bool> startPulling = new NetworkVariable<bool>();
    private readonly NetworkVariable<ulong> playerNetId = new NetworkVariable<ulong>();

    [Header("spawning the target")]
    public Transform targetPrefab;
    Transform spawnedTarget;

    [Header("moving")]
    public float speed;
    public Rigidbody2D rb;

    [Header("spawning the rope")]
    public GameObject ropeSegment;
    public Point ropeRenderer;

    public float distanceBetweenSegments;
    public float maxRopeLength;

    [Header("displaying the line")]
    public curveArm bezierCurveMaker;
    public float qualityMultiplier;

    [Header("hit detection")]
    public float radius;
    public float length;
    public LayerMask groundMask;

    [Header("pulling back")]
    public float tolerance;
    public float timeToPull;
    public float timeToHideRope;

    [Header("grappling")]
    public float force;
    bool appliedForce;
    knockable player;
    Vector3 vel = Vector2.zero;

    public void Initialize(ulong ID)
    {
        playerNetId.Value = ID;
        getPlayer(ID);
    }

    public override void OnNetworkSpawn()
    {
        spawnedTarget = Instantiate(targetPrefab, transform.position, Quaternion.identity);
        ropeRenderer.Target = spawnedTarget.transform;
        ropeRenderer.Anchor = transform;

        playerNetId.OnValueChanged += OnPlayerIdChanged;
        if (playerNetId.Value != 0)
        {
            getPlayer(playerNetId.Value);
        }

        if (IsServer)
        {
            rb.linearVelocity = transform.right * speed;
        }

        for (int i = 0; i < Mathf.CeilToInt(maxRopeLength / distanceBetweenSegments); i++)
        {
            GameObject newSegment = Instantiate(ropeSegment, transform.position, Quaternion.identity, transform);
            ropeRenderer.Segments.Add(new Point.segment { transform = newSegment.transform, distance = distanceBetweenSegments });
            bezierCurveMaker.points.Add(newSegment.transform);
        }

        bezierCurveMaker.steps = (int)(bezierCurveMaker.points.Count * qualityMultiplier);
    }

    private void OnPlayerIdChanged(ulong previousValue, ulong newValue)
    {
        getPlayer(newValue);
    }
    void getPlayer(ulong id)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject playerNGO))
        {
            player = playerNGO.GetComponent<knockable>();
        }
    }

    private void Update()
    {
        if (player == null || spawnedTarget == null) return;

        spawnedTarget.position = player.transform.position;

        if (IsServer)
        {
            handlePhysics();
        }
        if (startPulling.Value)
        {
            handlePulling();
        }
    }

    private void handlePhysics()
    {
        if (!appliedForce)
        {
            Collider2D other = Physics2D.OverlapCapsule(transform.position, new Vector2(radius, length), CapsuleDirection2D.Horizontal, Mathf.Atan2(transform.right.y, transform.right.x), groundMask);

            if (other != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.gravityScale = 0;
                appliedForce = true;

                Vector2 knockbackDirection = (transform.position - player.transform.position).normalized;
                applyKnockbackClientRpc(knockbackDirection * force);

                startPulling.Value = true;
            }
        }

        float currentDist = Vector2.Distance(player.transform.position, transform.position);
        if (!startPulling.Value && currentDist >= maxRopeLength - distanceBetweenSegments - tolerance)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0;
            startPulling.Value = true;
        }

        if (startPulling.Value == true)
        {
            transform.position = Vector3.SmoothDamp(transform.position, spawnedTarget.position, ref vel, timeToPull);

            if (Vector3.Distance(transform.position, spawnedTarget.position) < 1f || ropeRenderer.Segments[0].distance <= 0.75f)
            {
                GetComponent<NetworkObject>().Despawn();
            }
        }
    }

    private void handlePulling()
    {
        for (int i = 0; i < ropeRenderer.Segments.Count; i++)
        {
            float currentDistance = ropeRenderer.Segments[i].distance;
            ropeRenderer.Segments[i] = new Point.segment { transform = ropeRenderer.Segments[i].transform, distance = Mathf.MoveTowards(currentDistance, 0f, timeToHideRope * Time.deltaTime) };
        }
    }

    [ClientRpc]
    private void applyKnockbackClientRpc(Vector2 forceVector)
    {
        if (player.GetComponent<NetworkObject>().IsOwner)
        {
            player.KnockBack(forceVector, player.GetComponent<playerMovement>());
        }
    }

    public override void OnNetworkDespawn()
    {
        playerNetId.OnValueChanged -= OnPlayerIdChanged;
        if (spawnedTarget != null)
        {
            Destroy(spawnedTarget.gameObject);
        }
    }
}