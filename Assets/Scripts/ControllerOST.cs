using System.Collections.Generic;
using UnityEngine;

public class ControllerOST : MonoBehaviour
{
    public static bool isPlayingStealth = false;
    public List<AudioClip> stealthModeClips = new List<AudioClip>();
    public List<AudioClip> actonModeClips = new List<AudioClip>();

    public  AudioSource audioSource;
    private static ControllerOST _instance;
    public static ControllerOST Instance { get { return _instance; } }

    void Start()
    {
        PlayStealth();
    }

    public void Awake()
    {
        if (_instance != null && _instance != this)
            Destroy(this.gameObject);
        else
            _instance = this;
    }
    public void PlayStealth()
    {
        if (isPlayingStealth) return;
        isPlayingStealth = true;
        audioSource.clip = stealthModeClips[ClassicRandom.Range(0, stealthModeClips.Count)];
        audioSource.Play();

    }

    public void PlayAction()
    {
        if (!isPlayingStealth) return;
        isPlayingStealth = false;
        audioSource.clip = actonModeClips[ClassicRandom.Range(0, actonModeClips.Count)];
        audioSource.Play();
    }

    public void Stop()
    {
        audioSource.Stop();
    }
}
