using System;
using EMILtools.Core;
using EMILtools.Extensions;
using EMILtools.Systems;
using EMILtools.Timers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using static EMILtools.Timers.TimerUtility;


public interface IEntityFacade : IFacade, ITimerUser { }

public interface IEntityCtx : IContextViewImmutable
{
    public enum FaceDirection { Left, Right }
    public bool invulnerable { get; set; }
    public Enum currentHealthState { get; set; }
    public FaceDirection facingDirection { get; set; }
}

public interface IEntityBlackboard : IBlackboard
{
    public CountdownTimer invulnerableTimer { get; set; }
    public LivingEntity livingEntity { get; }
    public Rigidbody2D rb { get; }
    public AttackingBoundsChecker[] attackingBoundsCheckers { get; }
    public DamageFlasher damageFlasher { get; }
    public Transform statIndicatorsParent { get; }
    public GameObject armoredStatIndicatorPrefab { get; }
    public SoundConfig soundConfig { get; }
    public AudioSource audioSource {get; }

}

public interface IEntityConfig : IConfig
{
    [Serializable]
    public struct TakeDmg
    {
        [field: SerializeField] public float invulnerablePeriod { get; private set; } 
        
        // These two are set velocity directly for better feeling reactivity
        [field: SerializeField] public float pushForceX { get; set; }
        [field: SerializeField] public float pushForceY { get; private set; }
    }
    [Serializable]
    public struct HitStop
    {
        [field: SerializeField] [field: Range(0, 1)] public float scalar { get; private set; }
        [field: SerializeField] public float period { get; private set; }
    }
    [Serializable]
    public struct ClampLateralMov
    {
        // Ensure this is bigger than TakeDmg.pushForceX because that sets velocity directly
        [field: SerializeField] public float maxVelocityX { get; private set; }
    }
    
    public ClampLateralMov clampLateralMove { get; set; }
    public HitStop hitStop { get; set; }
    public TakeDmg takeDmg { get; set; }
}


public class SharedFMs
{
    
    public class ClampLateralMovement<TFacade> : UnboundFunctionality<TFacade, IEntityCtx>,
        UPDATE<IEntityCtx>
    where TFacade : IEntityFacade
    {
        public ClampLateralMovement(TFacade facade) : base(facade) { }

        IEntityConfig cfg => facade.API_Config<IEntityConfig>(); IEntityBlackboard bb => facade.API_Blackboard<IEntityBlackboard>(); IEntityCtx mutateCtx => facade.API_Context<IEntityCtx>();
        public override PipelineBuilder<IEntityCtx> InjectSteps(PipelineBuilder<IEntityCtx> builder)
            => builder.Add_ShortCircuit(new FuncCtxPredicate<IEntityCtx>(ctx => ctx.invulnerable));

        public override void Execute(IEntityCtx ctx)
        {
            if (ctx.invulnerable) return;
            float clampedX = Mathf.Clamp(bb.rb.linearVelocity.x, -cfg.clampLateralMove.maxVelocityX, cfg.clampLateralMove.maxVelocityX);
            float currentY = bb.rb.linearVelocity.y;
            bb.rb.linearVelocity = new Vector2(clampedX, currentY);
        }
    }
    
    public class InjectCtxIntoBoundsChecker<TFacade> : UnboundFunctionality<TFacade, IEntityCtx>
        where TFacade : IEntityFacade
    {
        public InjectCtxIntoBoundsChecker(TFacade facade) : base(facade) { }
        IEntityConfig cfg => facade.API_Config<IEntityConfig>(); IEntityBlackboard bb => facade.API_Blackboard<IEntityBlackboard>(); IEntityCtx mutateCtx => facade.API_Context<IEntityCtx>();

        protected override void Awake()
        {
            for (int i = 0; i < bb.attackingBoundsCheckers.Length; i++)
            {
                if(bb.attackingBoundsCheckers[i].enter) bb.attackingBoundsCheckers[i].enterContext.attackerEntityCtx = mutateCtx;
                if(bb.attackingBoundsCheckers[i].exit) bb.attackingBoundsCheckers[i].exitContext.attackerEntityCtx = mutateCtx;
                if(bb.attackingBoundsCheckers[i].stay) bb.attackingBoundsCheckers[i].stayContext.attackerEntityCtx = mutateCtx;
            }
        }
    }
    
    
    public class TakeDmg<TFacade> : BoundSetFunctionality<TFacade, IEntityCtx, TakeDmg<TFacade>.Setter>,
        ON_SET
            where TFacade : IEntityFacade
    {
        IEntityConfig cfg => facade.API_Config<IEntityConfig>(); IEntityBlackboard bb => facade.API_Blackboard<IEntityBlackboard>(); IEntityCtx mutateCtx => facade.API_Context<IEntityCtx>();

        public class Setter : DataSetter<AttackingBoundsChecker.AttackCtx>
        {
            [ShowInInspector] public AttackingBoundsChecker.AttackCtx AttackCtx => Get;
        }

        PersistentAction postCb;
        FuncPredicate BlockDamage;
        MovingEffect armoredIndicatorMEffect;

        public TakeDmg(IPublisher publisher, TFacade facade, PersistentAction postCb, FuncPredicate blockDamage = null)
            : base(publisher, facade)
        {
            BlockDamage = blockDamage;
            this.postCb = postCb;
        }

        protected override void Awake()
        {
            bb.invulnerableTimer = new CountdownTimer(cfg.takeDmg.invulnerablePeriod);
            facade.InitTimer(bb.invulnerableTimer, true);
            bb.invulnerableTimer.OnTimerStop.Add(InvulnerablitityEnd);
            
            PersistentAction<Enum> newHealthState = new PersistentAction<Enum>(NewHealthState);
            bb.livingEntity.SetupHealth();
            bb.livingEntity.SetAllThresholdCallbacks(newHealthState);
            bb.livingEntity.healthThresholds.GetNearestLastThreshold(bb.livingEntity.health, out var hpState);
            Debug.Log("PPS HP Index State: " + hpState + " w/ hp of " + bb.livingEntity.health.Value + "");
            
            mutateCtx.currentHealthState = hpState;
            
        }

        public void MutateUsingNewSetValues()
        {
            if (mutateCtx.invulnerable) return;
            string stateName = mutateCtx.currentHealthState.ToString();
            if (stateName == "Dying") return;
            string attackerStateName = SetContext.AttackCtx.attackerEntityCtx.currentHealthState.ToString();
            if (attackerStateName == "Dying") return;
            if (attackerStateName == "Dead") return;
            

            if (BlockDamage != null && BlockDamage.Evaluate())
            {
                bb.damageFlasher.Flash(DamageFlasher.FlashType.Blocked);
                switch (bb.soundConfig)
                {
                    case EnemiesSoundConfig cfg:
                        cfg.Play(bb.audioSource, EnemiesSoundConfig.EnemiesSounds.Immune);
                        break;
                }
                
                if (armoredIndicatorMEffect == null)
                {
                    armoredIndicatorMEffect = GameObject.Instantiate(bb.armoredStatIndicatorPrefab, bb.statIndicatorsParent).Get<MovingEffect>();
                    armoredIndicatorMEffect.transform.localPosition = Vector3.zero;
                }
                armoredIndicatorMEffect.Move();
            }
            else
                bb.damageFlasher.Flash(DamageFlasher.FlashType.Damage);
            
            bb.invulnerableTimer.StartAndReset();
            mutateCtx.invulnerable = true;
            bb.livingEntity.TakeDamageCaller.Invoke(SetContext.AttackCtx.damageInfo);
            SlowTime();

            switch (bb.soundConfig)
            {
                case EnemiesSoundConfig cfg:
                    cfg.Play(bb.audioSource, EnemiesSoundConfig.EnemiesSounds.TakeDamage);
                    break;
                case PlayerSoundConfig cfg:
                    cfg.Play(bb.audioSource, PlayerSoundConfig.PlayerSounds.TakeDamage);
                    break;
                default:
                    Debug.LogError("Unknown sound config type for hit sound");
                    break;
            }
            
            bb.livingEntity.healthThresholds.GetNearestLastThreshold(bb.livingEntity.health, out var hpState);
            if (facade is ISignalReceiverT<LivingEntity.PhasedHealthThresholdEnum> receiver &&
                hpState is LivingEntity.PhasedHealthThresholdEnum phasedState)
            {
                Debug.Log("PHASE CHANGE STATE + " + phasedState);
                receiver.Send(phasedState);
            }
            
        }

        async void SlowTime()
        {
            await TimeManager.Instance.SlowTimeForSeconds(cfg.hitStop.period, cfg.hitStop.scalar, ApplyHitForce);
            postCb?.Invoke();
        }

        void ApplyHitForce()
        {
            bb.rb.linearVelocity = new Vector2(0f, bb.rb.linearVelocity.y);
            Vector2 pushDir = SetContext.AttackCtx.attackerEntityCtx.facingDirection == IEntityCtx.FaceDirection.Right ? Vector2.right : Vector2.left;
            Vector2 lateral = pushDir * cfg.takeDmg.pushForceX;
            Vector2 upwards = Vector2.up * cfg.takeDmg.pushForceY;
            bb.rb.linearVelocity = lateral + upwards;
        }

        void NewHealthState(Enum newHealthState)
        {
            Debug.Log("New Health State: " + newHealthState + "");
            mutateCtx.currentHealthState = newHealthState;
        }

        void InvulnerablitityEnd()
        {
            mutateCtx.invulnerable = false;
            Debug.Log("Invulnerable End");
        }
    }
}