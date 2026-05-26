using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class SoundUser : MonoBehaviour
{
    public SoundConfig soundConfig;
    public AudioSource audioSource;

    public bool playOnAwake;

    [ShowIf("playOnAwake")]
    [ValueDropdown("GetSoundOptions")]
    public string soundName;

    [ShowIf("playOnAwake")]
    [Range(0, 1)]
    public float startPercentage;

    [ShowIf("playOnAwake")]
    public bool loop;

    [ShowIf("playOnAwake")]
    public FadeData fadeData;

    private Coroutine activeFadeRoutine;

    private IEnumerable GetSoundOptions()
    {
        if (soundConfig == null) return null;
        return soundConfig.GetSoundNames();
    }

    private void Awake()
    {
        if (playOnAwake)
        {
            PlaySound();
        }
    }

    [Button("$PlayButtonLabel")]
    private void TogglePreview()
    {
        if (audioSource == null) return;

        if (audioSource.isPlaying)
        {
            StopSound();
        }
        else
        {
            PlaySound();
        }
    }

    private string PlayButtonLabel => (audioSource != null && audioSource.isPlaying) ? "Stop Preview" : "Play Preview";

    private void PlaySound()
    {
        if (soundConfig == null || audioSource == null) return;

        float startTime = 0;
        var clip = soundConfig.GetClip(soundName);
        if (clip != null) startTime = clip.length * startPercentage;

        soundConfig.Play(audioSource, soundName, loop, startTime);
        
        if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
        
        if (fadeData.useFadeIn || fadeData.useFadeOut)
        {
            activeFadeRoutine = StartCoroutine(FadeRoutine(startTime));
        }
    }

    private void StopSound()
    {
        if (audioSource != null) audioSource.Stop();
        if (activeFadeRoutine != null)
        {
            StopCoroutine(activeFadeRoutine);
            activeFadeRoutine = null;
        }
    }

    private IEnumerator FadeRoutine(float soundStartTime)
    {
        float startTime = Time.time;
        float targetVolume = audioSource.volume;

        while (true)
        {
            float elapsed = Time.time - startTime;
            float currentElapsed = elapsed + soundStartTime;
            float currentFadeVolume = targetVolume;

            if (fadeData.useFadeIn && currentElapsed < fadeData.fadeInRange.y)
            {
                float t = Mathf.InverseLerp(fadeData.fadeInRange.x, fadeData.fadeInRange.y, currentElapsed);
                currentFadeVolume = Mathf.Lerp(0, targetVolume, t);
            }

            if (fadeData.useFadeOut)
            {
                if (currentElapsed > fadeData.fadeOutRange.x)
                {
                    float t = Mathf.InverseLerp(fadeData.fadeOutRange.x, fadeData.fadeOutRange.y, currentElapsed);
                    currentFadeVolume = Mathf.Min(currentFadeVolume, Mathf.Lerp(targetVolume, 0, t));
                }
            }

            audioSource.volume = currentFadeVolume;

            if (fadeData.useFadeOut && currentElapsed >= fadeData.fadeOutRange.y)
            {
                audioSource.Stop();
                yield break;
            }

            if (!loop && audioSource.clip != null && currentElapsed >= audioSource.clip.length)
            {
                yield break;
            }

            yield return null;
        }
    }

    [System.Serializable]
    public struct FadeData
    {
        public bool useFadeIn;
        [ShowIf("useFadeIn")]
        [MinMaxSlider(0, 60, true)]
        public Vector2 fadeInRange;

        public bool useFadeOut;
        [ShowIf("useFadeOut")]
        [MinMaxSlider(0, 60, true)]
        public Vector2 fadeOutRange;
    }
}
