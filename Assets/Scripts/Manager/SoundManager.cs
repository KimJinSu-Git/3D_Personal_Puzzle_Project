using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("🔊 Audio Sources")]
    public AudioSource bgmSource;          
    public AudioSource sfxSource;      
    public AudioSource loopSfxSource;  
    public AudioSource breathSource;      
    public AudioSource footstepSource;     
    public AudioSource enemySfxSource;     

    [Header("🎵 Audio Clips")]
    public List<AudioClip> bgmClips;
    public List<AudioClip> sfxClips;

    private Dictionary<string, AudioClip> bgmDict = new();
    private Dictionary<string, AudioClip> sfxDict = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeDictionaries();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeDictionaries()
    {
        foreach (var clip in bgmClips)
        {
            if (!bgmDict.ContainsKey(clip.name))
                bgmDict.Add(clip.name, clip);
        }

        foreach (var clip in sfxClips)
        {
            if (!sfxDict.ContainsKey(clip.name))
                sfxDict.Add(clip.name, clip);
        }
    }

    // 배경음 재생
    public void PlayBGM(string name, bool loop = true)
    {
        if (bgmDict.TryGetValue(name, out var clip))
        {
            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.Play();
        }
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    // 단발 효과음 재생
    public void PlaySFX(string name)
    {
        if (sfxDict.TryGetValue(name, out var clip))
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    // 루프 효과음 재생 (상자 끌기 등)
    public void PlayLoopSFX(string name)
    {
        if (sfxDict.TryGetValue(name, out var clip))
        {
            if (loopSfxSource.isPlaying && loopSfxSource.clip == clip) return;
            loopSfxSource.clip = clip;
            loopSfxSource.loop = true;
            loopSfxSource.Play();
        }
    }

    public void StopLoopSFX()
    {
        if (loopSfxSource.isPlaying)
            loopSfxSource.Stop();
    }

    public bool IsLoopPlaying(string clipName)
    {
        return loopSfxSource.isPlaying && loopSfxSource.clip != null && loopSfxSource.clip.name == clipName;
    }

    // 숨소리 재생
    public void PlayBreath(string name, bool loop = true)
    {
        if (sfxDict.TryGetValue(name, out var clip))
        {
            if (breathSource.clip == clip && breathSource.isPlaying) return;
            breathSource.clip = clip;
            breathSource.loop = loop;
            breathSource.Play();
        }
    }

    public void StopBreath()
    {
        if (breathSource.isPlaying)
            breathSource.Stop();
    }

    // 발소리 재생
    public void PlayFootstep(string name)
    {
        if (sfxDict.TryGetValue(name, out var clip))
        {
            if (footstepSource.clip == clip && footstepSource.isPlaying) return;
            footstepSource.clip = clip;
            footstepSource.loop = true;
            footstepSource.Play();
        }
    }

    public void StopFootstep()
    {
        if (footstepSource.isPlaying)
            footstepSource.Stop();
    }

    // 적 전용 효과음
    public void PlayEnemySFX(string name)
    {
        if (sfxDict.TryGetValue(name, out var clip))
        {
            if (enemySfxSource.isPlaying && enemySfxSource.clip == clip) return;
            enemySfxSource.clip = clip;
            enemySfxSource.loop = true;
            enemySfxSource.Play();
            enemySfxSource.PlayOneShot(clip);
        }
    }

    public void StopEnemySFX()
    {
        if (enemySfxSource.isPlaying)
            enemySfxSource.Stop();
    }
}