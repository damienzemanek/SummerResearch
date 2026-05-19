using System;
using UnityEngine;

[CreateAssetMenu(fileName = "MenuSoundConfig", menuName = "ScriptableObjects/SoundConfigs/MenuSounds")]
public class MenuSoundConfig : SoundConfig
{
    public enum MenuSounds { MainMenuMusic, ButtonClick  }
    [SerializeField] public SoundHandle<MenuSounds> soundHandle;

    public override void Play(AudioSource source, string soundName, bool loop = false, float startTime = 0f)
    {
        if (Enum.TryParse<MenuSounds>(soundName, out var result))
        {
            soundHandle.Play(source, result, 1f, loop, startTime);
        }
    }

    public override string[] GetSoundNames() => Enum.GetNames(typeof(MenuSounds));

    public override AudioClip GetClip(string soundName)
    {
        if (Enum.TryParse<MenuSounds>(soundName, out var result))
        {
            return soundHandle.GetClip(result);
        }
        return null;
    }

    public override void GenerateSounds() => soundHandle.GenerateSounds();
}
