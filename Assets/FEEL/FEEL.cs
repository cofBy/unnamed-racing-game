using System.Collections;
using System;
using UnityEngine;

public class FEEL : MonoBehaviour
{
    public Sound[] PublicSounds;
    public static Sound[] sounds;

    private void Awake()
    {
        sounds = PublicSounds;
        foreach (Sound s in sounds)
        {
            s.Source = gameObject.AddComponent<AudioSource>();
            s.Source.clip = s.clip;

            s.Source.loop = s.Loop;
        }
    }

    public static void PlaySound(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.clip.name == name);

        s.Source.volume = UnityEngine.Random.Range(s.MinVolume, s.MaxVolume);
        s.Source.pitch = UnityEngine.Random.Range(s.MinPitch, s.MaxPitch);

        if (s != null)
        {
            s.Source.Play();
        }
    }

    public static void Particals(GameObject obj, Vector3 pos, Quaternion rot)
    {
        PoolManager.spawnObject(obj, pos, rot, PoolManager.PoolType.Particals);
    }

    public static IEnumerator Flash(Renderer rend, Material flashMaterial, float duration)
    {
        Material[] mats = new Material[rend.materials.Length];
        Material[] orgM = rend.materials;

        for (int i = 0; i < mats.Length; i++)
        {
            mats[i] = flashMaterial;
        }

        rend.materials = mats;

        yield return new WaitForSeconds(duration);

        rend.materials = orgM;
    }

    public static IEnumerator CameraShake(float intensity, float duration, Vector3 multiplayer)
    {
        float timer = duration;
        Vector3 org = Camera.main.transform.position;

        while (timer > 0)
        {
            Vector3 Offset = new Vector3(UnityEngine.Random.Range(-intensity, intensity) * multiplayer.x, UnityEngine.Random.Range(-intensity, intensity) * multiplayer.y, UnityEngine.Random.Range(-intensity, intensity) * multiplayer.z);

            Camera.main.transform.position = org + Offset;

            timer -= Time.deltaTime;
            yield return null;
        }
        Camera.main.transform.position = org;
    }

    public static IEnumerator TimeFreeze(float TimeLevel, float duration)
    {

        Time.timeScale = TimeLevel;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1;
    }

}
