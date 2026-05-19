using System;
using EMILtools.Systems;

[Serializable]
public class EnemyStructure : MonoStructure<
    EnemyBlackboard,
    EnemyContextData,
    IEnemyContextView>
{
}