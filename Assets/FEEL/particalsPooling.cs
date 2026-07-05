using UnityEngine;

public class particalsPooling : MonoBehaviour
{
    private void OnParticleSystemStopped()
    {
        PoolManager.ReturnToPool(gameObject, PoolManager.PoolType.Particals);
    }
}
