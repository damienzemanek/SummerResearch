using System;
using EMILtools.Systems;

[Serializable]
public class PlayerStructure : MonoStructure<
    PlayerBlackboard,
    PlayerContextData,
    IPlayerContextView>
{
}