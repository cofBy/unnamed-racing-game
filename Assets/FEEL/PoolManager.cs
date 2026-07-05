using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PoolManager : MonoBehaviour
{
    public bool _DontDestroyOnLoad = false;


    [Header("Holders")]
    GameObject Holder;

    static GameObject GameObjects;
    static GameObject Particals;
    static GameObject Bullets;
    static GameObject Ex;

    [Header("Dictionarys")]
    static Dictionary<GameObject, ObjectPool<GameObject>> ObjectPools;
    static Dictionary<GameObject, GameObject> CloneToPrefab;

    public enum PoolType {GameObject, Particals, Bullet, Ex}
    public static PoolType PoolingType;

    private void Awake()
    {
        ObjectPools = new Dictionary<GameObject, ObjectPool<GameObject>>();
        CloneToPrefab = new Dictionary<GameObject, GameObject>();

        SetupHolders();
    }
    void SetupHolders()
    {
        Holder = new GameObject("Pool");

        GameObjects = new GameObject("Random Pool");
        GameObjects.transform.parent = Holder.transform;

        Particals = new GameObject("Particals Pool");
        Particals.transform.parent = Holder.transform;

        Bullets = new GameObject("Bullets Pool");
        Bullets.transform.parent = Holder.transform;

        Ex = new GameObject("Ex Pool");
        Ex.transform.parent = Holder.transform;

        if (_DontDestroyOnLoad == true)
        {
            DontDestroyOnLoad(Bullets.transform.root);
        }
    }
    static void CreatePool(GameObject prefab, Vector3 pos, Quaternion rot, PoolType UsedType = PoolType.GameObject)
    {
        ObjectPool<GameObject> pool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                GameObject obj = Instantiate(prefab);
                obj.SetActive(false);

                GameObject holder = GetParentOf(UsedType);
                obj.transform.SetParent(holder.transform);

                return obj;
            },
            actionOnGet: (GameObject obj) =>
            {
            },
            actionOnRelease: (GameObject obj) =>
            {
                obj.SetActive(false);
            },
            actionOnDestroy: (GameObject obj) =>
            {
                if (CloneToPrefab.ContainsKey(obj))
                {
                    CloneToPrefab.Remove(obj);
                }
            });

        ObjectPools.Add(prefab, pool);
    }

    static GameObject GetParentOf(PoolType poolType)
    {
        switch (poolType)
        {
            case PoolType.GameObject:

                return GameObjects;

            case PoolType.Particals:
                return Particals;

            case PoolType.Bullet:

                return Bullets;
            case PoolType.Ex:

                return Ex;

            default:
                return null;
        }
    }

    public static T SpawnObject<T>(GameObject prefab, Vector3 pos, Quaternion rot, PoolType UsedType = PoolType.GameObject) where T : Object
    {
        if (ObjectPools.ContainsKey(prefab) == false)
        {
            CreatePool(prefab, pos, rot, UsedType);
        }

        GameObject obj = ObjectPools[prefab].Get();

        if (obj != null)
        {
            if (CloneToPrefab.ContainsKey(obj) == false)
            {
                CloneToPrefab.Add(obj, prefab);
            }

            obj.transform.position = pos;
            obj.transform.rotation = rot;

            obj.SetActive(true);

            if (typeof(T) == typeof(GameObject))
            {
                return obj as T;
            }

            T component = obj.GetComponent<T>();
            if (component == null)
            {
                Debug.Log(prefab.name + " this object doesn't have a component of type " + typeof(T));
                return null;
            }

            return component;
        }

        return null;
    }

    public static T SpawnObject<T>(T typePrefab, Vector3 pos, Quaternion rot, PoolType UsedType = PoolType.GameObject) where T : Component
    {
        return SpawnObject<T>(typePrefab.gameObject, pos, rot, UsedType);
    }

    public static GameObject spawnObject(GameObject prefab, Vector3 pos, Quaternion rot, PoolType UsedType = PoolType.GameObject)
    {
        return SpawnObject<GameObject>(prefab, pos, rot, UsedType);
    }

    public static void ReturnToPool(GameObject obj, PoolType UsedType = PoolType.GameObject)
    {
        if (CloneToPrefab.TryGetValue(obj, out GameObject prefap))
        {
            GameObject parent = GetParentOf(UsedType);

            if (obj.transform.parent != parent.transform)
            {
                obj.transform.SetParent(parent.transform);
            }

            if (ObjectPools.TryGetValue(prefap, out ObjectPool<GameObject> pool))
            {
                pool.Release(obj);
            }
        }
        else
        {
            Debug.LogWarning("can't return an un-pooled object " + obj.name);
        }
    }
}
