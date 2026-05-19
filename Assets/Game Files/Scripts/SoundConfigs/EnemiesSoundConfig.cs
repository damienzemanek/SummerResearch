using System;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemiesSoundConfig", menuName = "ScriptableObjects/SoundConfigs/Enemies")]
public class EnemiesSoundConfig : SoundConfig
{
    public enum EnemiesSounds
    {
        TakeDamage, Attack, Immune, Yell, Walk, Stunned, Block
    }
    [SerializeField] public SoundHandle<EnemiesSounds> soundHandle;
    
    
    public void Play(AudioSource source, EnemiesSounds soundName, bool loop = false, float startTime = 0f)
        => soundHandle.Play(source, soundName, 1f, loop, startTime);

    public override void Play(AudioSource source, string soundName, bool loop = false, float startTime = 0)
    {
        if (Enum.TryParse<EnemiesSounds>(soundName, out var result))
        {
            soundHandle.Play(source, result, 1f, loop, startTime);
        }
    }

    public override string[] GetSoundNames() => Enum.GetNames(typeof(EnemiesSounds));

    public override AudioClip GetClip(string soundName)
    {
        if (Enum.TryParse<EnemiesSounds>(soundName, out var result))
        {
            return soundHandle.GetClip(result);
        }
        return null;
    }

    public override void GenerateSounds() => soundHandle.GenerateSounds();
}
