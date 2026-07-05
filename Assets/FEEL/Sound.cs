using UnityEngine;

[System.Serializable]
public class Sound
{
    public AudioClip clip;

    public float MinVolume;
    public float MaxVolume;

    public float MinPitch;
    public float MaxPitch;

    public bool Loop;

    [HideInInspector] public AudioSource Source;
}
