using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public struct SoundState<TSoundEnum>
    where TSoundEnum : Enum
{
    public AudioClip clip;
    public TSoundEnum soundEnum;

    public SoundState(AudioClip clip, TSoundEnum soundEnum)
    {
        this.clip = clip;
        this.soundEnum = soundEnum;
    }
}

[LabelWidth(125)]
[InlineProperty]
[Serializable]
public class SoundHandle<TSoundEnum>
    where TSoundEnum : Enum
{
    [LabelWidth(150)] public SoundState<TSoundEnum>[] Sounds;

    Dictionary<TSoundEnum, AudioClip> sounds;

    void Initialize()
    {
        sounds = new Dictionary<TSoundEnum, AudioClip>();
        if (Sounds != null)
        {
            foreach (var sound in Sounds)
            {
                if (sound.soundEnum != null && !sounds.ContainsKey(sound.soundEnum))
                    sounds.Add(sound.soundEnum, sound.clip);
            }
        }
    }

    public AudioClip GetClip(TSoundEnum soundEnum)
    {
        if (sounds == null) Initialize();
        if (sounds.TryGetValue(soundEnum, out var clip)) return clip;
        return null;
    }

    public void Play(AudioSource source, TSoundEnum soundEnum, float volume = 1f, bool loop = false, float startTime = 0f)
    {
        var clip = GetClip(soundEnum);
        if (clip != null && source != null)
        {
            if (loop)
            {
                if (source.isPlaying && source.loop && source.clip == clip) return;

                source.clip = clip;
                source.loop = true;
                source.volume = volume;
                source.time = Mathf.Clamp(startTime, 0, clip.length - 0.001f);
                source.Play();
            }
            else
            {
                source.PlayOneShot(clip, volume);
                Debug.Log("TEST PLAY ONESHOT: " + clip.name);
            }
        }
    }

    public void StopIfLooping(AudioSource source, TSoundEnum soundEnum)
    {
        var clip = GetClip(soundEnum);
        if (source == null || clip == null || source.clip != clip || !source.loop) return;
        source.loop = false;
        source.clip = null;
    }

    [Button]
    public void GenerateSounds()
    {
        var enumValues = (TSoundEnum[])Enum.GetValues(typeof(TSoundEnum));
        var newSounds = new List<SoundState<TSoundEnum>>();

        foreach (var value in enumValues)
        {
            AudioClip existingClip = null;
            if (Sounds != null)
            {
                foreach (var s in Sounds)
                {
                    if (EqualityComparer<TSoundEnum>.Default.Equals(s.soundEnum, value))
                    {
                        existingClip = s.clip;
                        break;
                    }
                }
            }
            newSounds.Add(new SoundState<TSoundEnum>(existingClip, value));
        }

        Sounds = newSounds.ToArray();
    }
}

[Serializable]
public abstract class SoundConfig : ScriptableObject
{
    public abstract void Play(AudioSource source, string soundName, bool loop = false, float startTime = 0f);
    public abstract string[] GetSoundNames();
    public abstract AudioClip GetClip(string soundName);

    [Button]
    public abstract void GenerateSounds();
}
