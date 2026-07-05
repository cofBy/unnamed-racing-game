using Unity.Netcode;
using UnityEngine;

public class knockable : NetworkBehaviour
{
    public Vector2 targetVel;

    [Header("self KnockBack")]
    public Vector2 knockBackFalloff;

    [Header("spawning partical")]
    public GameObject body;
    public Material bodySprite;
    public void KnockBack(Vector2 dir, playerMovement movement)
    {
        targetVel += dir;
        movement.gPull = 0;
    }
    private void FixedUpdate()
    {
        if (IsOwner == false || IsSpawned == false) return;

        targetVel.x = shrinkX(targetVel.x, knockBackFalloff.x);
        targetVel.y = shrinkX(targetVel.y, knockBackFalloff.y);
    }
    float shrinkX(float x, float multiplier)
    {
        return x > 0 ? Mathf.Max(x - multiplier, 0) : Mathf.Min(x + multiplier, 0);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("lava"))
        {
            die();
        }
    }

    void die()
    {
        ParticleSystemRenderer bodyInstace = PoolManager.spawnObject(body, transform.position, new Quaternion()).GetComponent<ParticleSystemRenderer>();
        bodyInstace.material = bodySprite;
        transform.position = Vector3.zero;
    }
}
