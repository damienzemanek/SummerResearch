using System;
using UnityEngine;
using EMILtools.Systems;
using Pathfinding;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "EMILtools/ScriptableObjects/Configs/Enemy")]
public class EnemyConfig : Config, IEntityConfig
{
    public enum EnemyAnims { Idle, Attack, Walk, Dying, Stunned, Blocking, Yell, ForwardAttack }
    public enum EnemyBlendVars { }
    
    [field: SerializeField] public AnimHandle<EnemyAnims, EnemyBlendVars> animHandle { get; private set; }
    [field: SerializeField] public Follow follow { get; private set; }
    [field: SerializeField] public InAir inAir { get; private set; }
    [field: SerializeField] public IEntityConfig.ClampLateralMov clampLateralMove { get; set; }
    [field: SerializeField] public Jump jump { get; private set; }
    [field: SerializeField] public ViewRange viewRange { get; private set; }
    [field: SerializeField] public DyingState dyingState { get; private set; }
    [field: FormerlySerializedAs("<timeSlowTakeDmg>k__BackingField")] [field: PropertyOrder(-1)] [field: SerializeField] public IEntityConfig.HitStop hitStop { get; set; }
    [field: PropertyOrder(-1)] [field: SerializeField] public IEntityConfig.TakeDmg takeDmg { get; set; }
    [field: SerializeField] public Stunned stunned { get; private set; }
    [field: SerializeField] public Finishable finishable { get; private set; }
    [field: SerializeField] public HyperArmor hyperArmor { get; private set; }
    [field: SerializeField] public DescisionMaking decisionMaking { get; private set; }
    [field: SerializeField] public Yell yell { get; private set; }
    [field: SerializeField] public ForwardAttack forwardAttack { get; private set; }
    
    [Serializable]
    public struct ForwardAttack
    {
        [field: SerializeField] public bool canForwardAttack { get; private set; }
        [field: SerializeField] public float forwardAttackForce { get; private set; }
    }
    
    [Serializable]
    public struct Yell
    {
        [field: SerializeField] public bool yellAfterPhaseChange { get; private set; }
    }

    [Serializable]
    public struct DescisionMaking
    {
        [field: SerializeField] public float blockWaitTime { get; private set; }
        [field: SerializeField] public float fwdAttackTime { get; private set; }

    }
    
    [Serializable]
    public struct HyperArmor
    {
        [field: SerializeField] public bool useHyperArmor { get; private set; }
    }
    
    [Serializable]
    public struct Finishable
    {
        [field: SerializeField] public Ref<float> finishTime { get; private set; }
    }
    
    [Serializable]
    public struct Stunned
    {
        [field: SerializeField] public bool canBeStunned { get; private set; }
        [field: SerializeField] public Ref<float> stunnedPeriod { get; private set; }
    }
    
    [Serializable]
    public struct DyingState
    {
        [field: SerializeField] public float dyingStatePeriod { get; private set; }
        [field: SerializeField] public float outOfDeathStateHealAmount { get; private set; }

    }

    [Serializable]
    public struct ViewRange
    {
        [field: SerializeField] public Ref<float> delayToStopChasing { get; private set; }
    }
    
    [Serializable]
    public struct Jump
    {
        [field: SerializeField] public bool canJump { get; private set; }
        [field: SerializeField] public float jumpForceScalar { get; private set; }
        [field: SerializeField] public Ref<float> jumpDelay { get; private set;}
    }
    
    
    [Serializable]
    public struct Follow
    {
        [field: SerializeField] public float speedScalar { get; private set; }   
        [field: SerializeField] public float nextWaypointDistance { get; private set; }  
        [field: SerializeField] public float stoppingDistToTarget { get; private set; }  
        [field: SerializeField] public float minHorizAngleToFollow { get; private set; } 

    }

    [Serializable]
    public struct InAir
    {
        [field: SerializeField] public float checkDist { get; private set; } 
        [field: SerializeField] public LayerMask mask { get; private set; }
        [field: SerializeField] public float fallScalar { get; private set; }
        [field: SerializeField] public ForceMode2D forceMode { get; private set; }
    }
}