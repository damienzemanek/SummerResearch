using System;
using EMILtools.Systems;
using EMILtools.Timers;
using Pathfinding;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Splines;
using static LifecycleEX;

[Serializable]
public class EnemyBlackboard : Blackboard, IEntityBlackboard
{
    IEntityBlackboard _iEntityBlackboardImplementation;
    [field: SerializeField] [field: Required] public Animator animator { get; private set; }
    [field: SerializeField] [field: Required] public Seeker seeker { get; private set; }
    [field: SerializeField] [field: Required] public Rigidbody2D rb { get; private set; }
    [field: SerializeField] [field: Required] public AttackingBoundsChecker[] attackingBoundsCheckers { get; private set; }
    [field: SerializeField] [field: Required] public DamageFlasher damageFlasher { get; private set; }
    [field: SerializeField] [field: Required] public Transform statIndicatorsParent { get; private set; }
    [field: SerializeField] [field: RequiredIn(PrefabKind.InstanceInScene)] public Transform target { get; private set; }
    [field: SerializeField] [field: Required] public DelayLimitedMethod computePath { get; private set; }
    [field: SerializeField] [field: Required] public Transform[] feetPoints { get; private set; }
    
    
    [field: SerializeField] [field: Required] public CountdownTimer jumpTimer { get; set; }
    [field: SerializeField] [field: Required] public CountdownTimer invulnerableTimer { get; set; }
    [field: SerializeField] [field: Required] public CountdownTimer dyingStateTimer { get; set; }
    [field: SerializeField] [field: Required] public CountdownTimer stunnedTimer { get; set; }
    [field: SerializeField] [field: Required] public CountdownTimer finishTimer { get; set; }
    [field: SerializeField] [field: Required] public CountdownTimer blockWaitTimer { get; set; }
    [field: SerializeField] [field: Required] public CountdownTimer fwdAttackTimer { get; set; }
    
    [field: SerializeField] [field: Required] public Collider2D bodyCollider { get; private set; }
    [field: SerializeField] [field: Required] public GameObject armoredStatIndicatorPrefab { get; set; }
    [field: SerializeField] [field: Required] public SoundConfig soundConfig { get; private set; }
    [field: SerializeField] [field: Required] public AudioSource audioSource { get; set; }
    [field: SerializeField] [field: Required] public EnemiesSoundConfig enemiesSoundConfig { get; private set; }
    [field: SerializeField] [field: Required] public Transform faceDirTransform { get; private set; }
    [field: SerializeField] [field: Required] public FinisherEvent finisherEvent { get; private set; }
    [field: SerializeField] [field: Required] public LivingEntity livingEntity { get; private set; }
    [field: SerializeField] [field: Required] public Behaviour viewRange { get; private set; }
    
}