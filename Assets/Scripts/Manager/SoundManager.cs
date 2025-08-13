using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    public AudioSource loopSfxSource;
    public AudioSource breathSource;
    public AudioSource footstepSource;
    public AudioSource enemySfxSource;

    [Header("Audio Clips")]
    public List<AudioClip> bgmClips;
    public List<AudioClip> sfxClips;

    private Dictionary<string, AudioClip> bgmDict = new();
    private Dictionary<string, AudioClip> sfxDict = new();

    private struct PausedAudio
    {
        public AudioClip clip;
        public float time;
    }

    private Dictionary<AudioSource, PausedAudio> pausedAudioSources = new();

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

    public void PlaySFX(string name, bool loop = false)
    {
        if (sfxDict.TryGetValue(name, out var clip))
        {
            if (sfxSource.clip == clip && sfxSource.isPlaying)
                return;

            sfxSource.clip = clip;
            sfxSource.loop = loop;
            sfxSource.Play();
        }
    }

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

    public void PauseAllExceptBGM()
    {
        pausedAudioSources.Clear();

        PauseSource(sfxSource);
        PauseSource(loopSfxSource);
        PauseSource(breathSource);
        PauseSource(footstepSource);
        PauseSource(enemySfxSource);
    }

    private void PauseSource(AudioSource source)
    {
        if (source.isPlaying && source.clip != null)
        {
            pausedAudioSources[source] = new PausedAudio
            {
                clip = source.clip,
                time = source.time
            };
            source.Pause();
        }
    }

    public void ResumePausedSFX()
    {
        foreach (var kvp in pausedAudioSources)
        {
            var source = kvp.Key;
            var paused = kvp.Value;

            source.clip = paused.clip;
            source.time = paused.time;
            source.Play();
        }

        pausedAudioSources.Clear();
    }
}