using System;
using System.Collections;
using EMILtools_Private.Core;
using EMILtools.Core;
using EMILtools.Extensions;
using EMILtools.Systems;
using EMILtools.Timers;
using Pathfinding;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;
using static AttackingBoundsChecker;
using static LivingEntity;
using Random = System.Random;

public class EnemyFunctionality : Functionalities<
    EnemyController,
    IEnemyContextView>
{
    
    protected override IState AddModulesHere(EnemyController f)
    {
        EnemyConfig cfg = facade.API_Config<EnemyConfig>();
        EnemyBlackboard bb = facade.API_Blackboard<EnemyBlackboard>();
        EnemyContextData mutateCtx = facade.API_Context<EnemyContextData>();

        // Add Modules like this
        // AddModule(new ExampleModule(...));

        // Return the module that is the starting state, ex:
        // return AddModule(new Idle(...))

        AddModule(new AddBodyColliderExclusionLayer(facade.Actions.AddBodyColliderExclusionLayer, f));
        AddModule(new ForwardAttack(facade));
        AddModule(new Yell(facade.Actions.Yell, facade));
        AddModule(new Blocking(facade));
        AddModule(new DescisionMaker(facade));
        AddModule(new AttackRangeDetector(facade.Actions.AttackInRange, f));
        AddModule(new HyperArmor(facade.Actions.ToggleHyperArmor, f));
        AddModule(new FinisherInputAvaliable(facade.Actions.FinisherInputAvaliable, f));
        AddModule(new Hooked(facade.Actions.isHookedBySomething, f));
        AddModule(new Stunnable(facade.Actions.Stun, f));
        AddModule(new SharedFMs.InjectCtxIntoBoundsChecker<EnemyController>(f));
        AddModule(new DyingState(f));
        AddModule(new SharedFMs.TakeDmg<EnemyController>(facade.Actions.TakeDamage, f, null, 
            new FuncPredicate(() => mutateCtx.yelling || cfg.hyperArmor.useHyperArmor && mutateCtx.hyperArmorUsableInState && mutateCtx.hyperArmorWindowActive)));
        AddModule(new FaceDir(f));
        AddModule(new Follow(f));
        AddModule(new ViewRange(facade.Actions.CanSeeTarget, f));
        AddModule(new Jump(f));
        AddModule(new SharedFMs.ClampLateralMovement<EnemyController>(f));
        AddModule(new InAir(f));
        return AddModule(new Idle(f));

    }
    protected override void SetupTransitionsForFSM(StateMachine<IEnemyContextView> fsm, IEnemyContextView ctx)
    {
        // Add State Transitions here
        // fsm.AddAnyTransition<Jump>(new FuncPredicate(() => ctx.isJumping), "Jumping");
        EnemyConfig cfg = facade.API_Config<EnemyConfig>();
        EnemyContextData mutateCtx = facade.API_Context<EnemyContextData>();

        
        fsm.AddTransition<Idle, Follow>(new FuncCtxPredicate<IEnemyContextView>(ctx => ctx.canSeeTarget), "Can See Target");
        fsm.AddTransition<Follow, Idle>(new FuncCtxPredicate<IEnemyContextView>(ctx => !ctx.canSeeTarget), "Can't See Target");
        
        fsm.AddAnyTransition<DyingState>(new FuncCtxPredicate<IEnemyContextView>(ctx => ctx.currentHealthState.ToString() == "Dying" && !ctx.isBeingFinished), "Dying");
        fsm.AddTransition<DyingState, Follow>(new FuncCtxPredicate<IEnemyContextView>(ctx => ctx.currentHealthState.ToString() != "Dying"), "Not Dying");
        
        fsm.AddTransition<Follow, Blocking>(new FuncCtxPredicate<IEnemyContextView>(ctx => ctx.decidedToBlock), "Decided To Block");
        fsm.AddTransition<Blocking, Follow>(new FuncCtxPredicate<IEnemyContextView>(ctx => !ctx.decidedToBlock), "Decided To Block");
        
        fsm.AddTransition<Blocking, Stunnable>(new FuncCtxPredicate<IEnemyContextView>(ctx => cfg.stunned.canBeStunned && mutateCtx.stunWindowActive && ctx.isStunned), "Stunned");
        fsm.AddTransition<Stunnable, Follow>(new FuncCtxPredicate<IEnemyContextView>(ctx => !ctx.isStunned), "Not Stunned");
        
        fsm.AddTransition<DyingState, Hooked>(new FuncCtxPredicate<IEnemyContextView>(ctx => ctx.isBeingFinished), "Being Finished");
        fsm.AddTransition<Hooked, Follow>(new FuncCtxPredicate<IEnemyContextView>(ctx => !ctx.isBeingFinished), "Not Being Finished");
        
        fsm.AddTransition<Follow, Yell>(new FuncCtxPredicate<IEnemyContextView>(ctx => ctx.yelling), "Yelling");
        fsm.AddTransition<Yell, Follow>(new FuncCtxPredicate<IEnemyContextView>(ctx => !ctx.yelling), "Not Yelling");
        
        
        fsm.AddTransition<Follow, ForwardAttack>(new FuncCtxPredicate<IEnemyContextView>(ctx => cfg.forwardAttack.canForwardAttack && ctx.decidedToFwdAttack), "Decided To Forward Attack");
        fsm.AddTransition<ForwardAttack, Follow>(new FuncCtxPredicate<IEnemyContextView>(ctx => !ctx.decidedToFwdAttack), "Decided To Forward Attack");
    }


    public struct ResetBodyExclusionLayers { }
    public interface IAPI_ResetExclusionLayers : IAPI_Dependant<ResetBodyExclusionLayers> { }
    public class AddBodyColliderExclusionLayer : BoundSetFunctionality<EnemyController, IEnemyContextView, AddBodyColliderExclusionLayer.Setter>,
        ON_SET,
        IAPI_ResetExclusionLayers
    {
        EnemyConfig cfg => facade.API_Config<EnemyConfig>(); EnemyBlackboard bb => facade.API_Blackboard<EnemyBlackboard>(); EnemyContextData mutateCtx => facade.API_Context<EnemyContextData>();

        private LayerMask origExcludeLayers;
        
        [Serializable]
        public class Setter : DataSetter<string>
        {
            [ShowInInspector] public string excludedLayerName => Get;
        }
        public AddBodyColliderExclusionLayer(IPublisher publisher, EnemyController facade) : base(publisher, facade) { }

        protected override void Awake() 
            => origExcludeLayers = bb.bodyCollider.excludeLayers;

        public void MutateUsingNewSetValues()
        {
            // bb.bodyCollider.excludeLayers |= LayerMask.GetMask(SetContext.Get);
        }

        void IAPI_Dependant<ResetBodyExclusionLayers>.DependanciesReceived(ResetBodyExclusionLayers _)
            => bb.bodyCollider.excludeLayers = origExcludeLayers;
    }
    
    

    public class Yell : BoundSetFunctionality<EnemyController, IEnemyContextView, Yell.Setter>,
        ON_SET,
        FSM_STATE_ENTER<IEnemyContextView>
    {
        public Yell(IPublisher publisher, EnemyController facade) : base(publisher, facade) { }

        EnemyConfig cfg => facade.API_Config<EnemyConfig>(); EnemyBlackboard bb => facade.API_Blackboard<EnemyBlackboard>(); EnemyContextData mutateCtx => facade.API_Context<EnemyContextData>();
        
        [Serializable]
        public class Setter : DataSetter<bool>
        {
            [ShowInInspector] public bool yelling => Get;
        }

        public void MutateUsingNewSetValues()
        {
            if (!cfg.yell.yellAfterPhaseChange) return; 
            mutateCtx.yelling = SetContext.yelling;
        }

        // Stops yelling from animation event
        public void OnEnterState(IEnemyContextView ctx)
        {
            if(mutateCtx.isStunned)
                facade.StartCoroutine(C_WaitUntilNotStunnedIfStunned());
            else
            {
                bb.enemiesSoundConfig.Play(bb.audioSource, EnemiesSoundConfig.EnemiesSounds.Yell);
                facade.StartCoroutine(YellSFX());
            }
        }

        IEnumerator C_WaitUntilNotStunnedIfStunned()
        {
            while(mutateCtx.isStunned) yield return null;
            // beats making a yield return new
            yield return null;
            yield return null;
            yield return null;
            cfg.animHandle.Play(bb.animator, EnemyConfig.EnemyAnims.Yell);
            facade.StartCoroutine(YellSFX());
        }

        IEnumerator YellSFX()
        {
            yield return new WaitForSeconds(1f);
            cfg.animHandle.Play(bb.animator, EnemyConfig.EnemyAnims.Yell);
        }
    }

    public struct EnemyDescisions
    {
        public bool blockingAllowed;
        public bool fwdAttackingAllowed;
    }
    public interface IAPI_EnemyDescisionMaker : IAPI_Dependant<EnemyDescisions> { }
    class DescisionMaker : UnboundFunctionality<EnemyController, IEnemyContextView>, 
        IAPI_EnemyDescisionMaker
    {
        EnemyConfig cfg => facade.API_Config<EnemyConfig>(); EnemyBlackboard bb => facade.API_Blackboard<EnemyBlackboard>(); EnemyContextData mutateCtx => facade.API_Context<EnemyContextData>();
        public DescisionMaker(EnemyController facade) : base(facade) { }

        bool blockingAllowed = false;
        public bool fwdAttackingAllowed = false;


        protected override void Awake()
        {
            bb.blockWaitTimer = new CountdownTimer(cfg.decisionMaking.blockWaitTime);
            bb.fwdAttackTimer = new CountdownTimer(cfg.decisionMaking.fwdAttackTime);
            bb.blockWaitTimer.OnTimerStop.Add(() => mutateCtx.decidedToBlock = true);
            bb.fwdAttackTimer.OnTimerStop.Add(() => mutateCtx.decidedToFwdAttack = true);
            facade.InitTimer(bb.blockWaitTimer, true);
            facade.InitTimer(bb.fwdAttackTimer, true);
        }

        void IAPI_Dependant<EnemyDescisions>.DependanciesReceived(EnemyDescisions injectedContext)
        {
            if (!blockingAllowed)
            {
                blockingAllowed = injectedContext.blockingAllowed;

                if (injectedContext.blockingAllowed)
                {
                    bb.blockWaitTimer.StartAndReset();
                    Debug.Log("DSM : CAN NOW BLOCK");
                } 
            }

            if (!fwdAttackingAllowed)
            {
                fwdAttackingAllowed = injectedContext.fwdAttackingAllowed;

                if (injectedContext.fwdAttackingAllowed)
                {
                    bb.fwdAttackTimer.StartAndReset();
                    Debug.Log("DSM : CAN NOW FWD ATTACK");
                }
            }
        }
    }

    class Blocking : UnboundFunctionality<EnemyController, IEnemyContextView>,
        FSM_STATE_ENTER<IEnemyContextView>,
        FSM_STATE_EXIT<IEnemyContextView>
    {
        public Blocking(EnemyController facade) : base(facade) { }

        EnemyConfig cfg => facade.API_Config<EnemyConfig>(); EnemyBlackboard bb => facade.API_Blackboard<EnemyBlackboard>(); EnemyContextData mutateCtx => facade.API_Context<EnemyContextData>();
        
        public void OnEnterState(IEnemyContextView ctx)
        {
            cfg.animHandle.Play(bb.animator, EnemyConfig.EnemyAnims.Blocking);
            mutateCtx.stunWindowActive = true;
            mutateCtx.hyperArmorWindowActive = true;
            
            bb.fwdAttackTimer.Pause();
            facade.StartCoroutine(BlockSound());
        }

        IEnumerator BlockSound()
        {
            yield return new WaitForSeconds(0.83f);
            bb.enemiesSoundConfig.Play(bb.audioSource, EnemiesSoundConfig.EnemiesSounds.Block);
        }

        public void OnExitState(IEnemyContextView ctx)
        {
            mutateCtx.stunWindowActive = false;
            mutateCtx.hyperArmorWindowActive = false;
            
            bb.fwdAttackTimer.Resume();
        }
    }
    
    class ForwardAttack : UnboundFunctionality<EnemyController, IEnemyContextView>,
        FSM_STATE_ENTER<IEnemyContextView>,
        FSM_STATE_EXIT<IEnemyContextView>
    {
        public ForwardAttack(EnemyController facade) : base(facade) { }

        EnemyConfig cfg => facade.API_Config<EnemyConfig>(); EnemyBlackboard bb => facade.API_Blackboard<EnemyBlackboard>(); EnemyContextData mutateCtx => facade.API_Context<EnemyContextData>();
        
        Action EndAttackDelegateCached;
        protected override void Awake() => EndAttackDelegateCached = EndAttack;
        void EndAttack() => mutateCtx.attacking = false;

        
        public void OnEnterState(IEnemyContextView ctx)
        {
            cfg.animHandle.PlayThenOnEnd(bb.animator, EnemyConfig.EnemyAnims.ForwardAttack, EndAttackDelegateCached);

            var currentLookingDir = mutateCtx.facingDirection;
            Vector2 fwd = new Vector2(currentLookingDir == IEntityCtx.FaceDirection.Left ? -1 : 1, 0);
            bb.rb.AddForce(fwd * cfg.forwardAttack.forwardAttackForce, ForceMode2D.Impulse);
            mutateCtx.attacking = true;
            
            facade.StartCoroutine(C_LandNaiveImplm());
        }
        
// simple impl cause this games needs to be done
        IEnumerator C_LandNaiveImplm()
        {
            yield return new WaitForSeconds(0.5f);
            bb.enemiesSoundConfig.Play(bb.audioSource, EnemiesSoundConfig.EnemiesSounds.Attack);
            while (mutateCtx.decidedToFwdAttack)
            {
                yield return null;
                bb.rb.ResetVel2D();
            }
        }

        public void OnExitState(IEnemyContextView ctx)
        {
            bb.fwdAttackTimer.StartAndReset();
            mutateCtx.attacking = false;
        }
        
    }
    class AttackRangeDetector : BoundSetFunctionality<EnemyController, IEnemyContextView, AttackRangeDetector.Setter>,
        ON_SET
    {
        EnemyConfig cfg => facade.API_Config<EnemyConfig>(); EnemyBlackboard bb => facade.API_Blackboard<EnemyBlackboard>(); EnemyContextData mutateCtx => facade.API_Context<EnemyContextData>();
        [Serializable]
        public class Setter : DataSetter<bool>
        {
            bool inAttackRange => Get;
        }

        public AttackRangeDetector(IPublisher publisher, EnemyController facade) : base(publisher, facade) { }
        
        Action EndAttackDelegateCached;
        protected override void Awake() => EndAttackDelegateCached = EndAttack;

        /// <summary>
        /// Start Attacking - comes from the BoundSet Publisher
        /// </summary>
        public void MutateUsingNewSetValues()
        {
            if (mutateCtx.attacking == true) return;
            if(facade.FSM.CurrentStateType == typeof(Blocking)) return;
            if(facade.FSM.CurrentStateType == typeof(Stunnable)) return;
            if(facade.FSM.CurrentStateType == typeof(ForwardAttack)) return;
            if(facade.FSM.CurrentStateType == typeof(Yell)) return;


            mutateCtx.attacking = true;
            // Animation events will turn on and off the attacking bounds checker collider
            cfg.animHandle.PlayThenOnEnd(bb.animator, EnemyConfig.EnemyAnims.Attack, EndAttackDelegateCached);
            bb.enemiesSoundConfig.Play(bb.audioSource, EnemiesSoundConfig.EnemiesSounds.Attack);

        }
        void EndAttack() => mutateCtx.attacking = false;
    }
    

    class HyperArmor : BoundSetFunctionality<EnemyController, IEnemyContextView, HyperArmor.Setter>,
        ON_SET
    {
        public class Setter : DataSetter<bool>
        {
            [ShowInInspector] public bool hyperArmorActive => Get;
        }

        protected override void Awake() => mutateCtx.hyperArmorUsableInState = false;

        public HyperArmor(IPublisher publisher, EnemyController facade) : base(publisher, facade) { }
        EnemyConfig cfg => facade.API_Config<EnemyConfig>(); EnemyBlackboard bb => facade.API_Blackboard<EnemyBlackboard>(); EnemyContextData mutateCtx => facade.API_Context<EnemyContextData>();
        public void MutateUsingNewSetValues()
        {
            if (cfg.hyperArmor.useHyperArmor)
            {
                mutateCtx.hyperArmorWindowActive = SetContext.hyperArmorActive;
            }
            else
                mutateCtx.hyperArmorWindowActive = false;
        }
    }
    
    class FinisherInputAvaliable : BoundSetFunctionality<EnemyController, IEnemyContextView, FinisherInputAvaliable.Setter>, 
        ON_SET
    {
        EnemyConfig cfg => facade.API_Config<EnemyConfig>(); EnemyBlackboard bb => facade.API_Blackboard<EnemyBlackboard>(); EnemyContextData mutateCtx => facade.API_Context<EnemyContextData>();
        public class Setter : DataSetter<bool>
        {
            [ShowInInspector] public bool finisherInputAvaliable => Get;
        }
        public FinisherInputAvaliable(IPublisher publisher, EnemyController facade) : base(publisher, facade) { }

        public void MutateUsingNewSetValues()
        {
            mutateCtx.isFinisherInputAvaliable.Set(SetContext.finisherInputAvaliable);
            Debug.Log("Finisher Input Available: " + SetContext.finisherInputAvaliable);
        }
    }
    
    
    class Hooked : BoundSetFunctionality<EnemyController, IEnemyContextView, Hooked.Setter>,
        ON_SET,
        FSM_STATE_ENTER<IEnemyContextView>
    {
        EnemyConfig cfg => facade.API_Config<EnemyConfig>(); EnemyBlackboard bb => facade.API_Blackboard<EnemyBlackboard>(); EnemyContextData mutateCtx => facade.API_Context<EnemyContextData>();
        public class Setter : DataSetter<(bool isHooked, 
            Publisher<global::Hook.FinisherContext>)>
        {
            [ShowInInspector] public bool isHooked => Get.isHooked;
            [ShowInInspector] public Publisher<global::Hook.FinisherContext> hookingEntityResponse => Get.Item2;
        }

        PersistentAction hookingEntity_HookedBreakoutCallback = new();
        
        protected override void Awake()
        {
            bb.finishTimer = new CountdownTimer(cfg.finishable.finishTime);
            bb.finishTimer.OnTimerStop.Add(HookedBreakoutDyingState);
            facade.InitTimer(bb.finishTimer, true);
        }

        public Hooked(IPublisher publisher, EnemyController facade) : base(publisher, facade) { }
        
        public void MutateUsingNewSetValues()
        {
            if (facade.FSM.CurrentStateType != typeof(DyingState)) return;
            mutateCtx.isBeingFinished = SetContext.isHooked;

            // Hooking Stopped
            if (!SetContext.isHooked) bb.finisherEvent.StopEarly();
        }
        
        public void OnEnterState(IEnemyContextView ctx)
        {
            Debug.Log("AA" + SetContext.hookingEntityResponse);   
            SetContext.hookingEntityResponse.Publish(new global::Hook.FinisherContext(SetContext.isHooked, bb.finishTimer, hookingEntity_HookedBreakoutCallback, mutateCtx.isFinisherInputAvaliable, bb.livingEntity, bb.finisherEvent));
            bb.finisherEvent.StartEvent();
            Debug.Log("Hooked");
        }
        
        void HookedBreakoutDyingState()
        {
            if(bb.livingEntity.isDead) return;
            bb.finisherEvent.Stop();
            bb.dyingStateTimer.Time = 0;
            hookingEntity_HookedBreakoutCallback.Invoke();
            Debug.Log("Finisher: Hooked Breakout Dying State");
        }
    }

    
    class Stunnable : BoundSetFunctionality<EnemyController, IEnemyContextView, Stunnable.Setter>,
        FSM_STATE_ENTER<IEnemyContextView>,
        FSM_STATE_EXIT<IEnemyContextView>,
    ON_SET
    {
        EnemyConfig cfg => facade.API_Config<EnemyConfig>(); EnemyBlackboard bb => facade.API_Blackboard<EnemyBlackboard>(); EnemyContextData mutateCtx => facade.API_Context<EnemyContextData>();
        public class Setter : DataSetter<bool>
        {
            [ShowInInspector] public bool isStunned => Get;
        }

        public Stunnable(IPublisher publisher, EnemyController facade) : base(publisher, facade) { }

        protected override void Awake()
        {
            bb.stunnedTimer = new CountdownTimer(cfg.stunned.stunnedPeriod);
            bb.stunnedTimer.OnTimerStop.Add(() => mutateCtx.isStunned = false);
            facade.InitTimer(bb.stunnedTimer, true);
        }
        int bodyAttkColliderIndex = 0;

        public void OnEnterState(IEnemyContextView ctx)
        {
            Debug.Log("Entered Stun State");
            bb.stunnedTimer.StartAndReset();
            mutateCtx.hyperArmorWindowActive = true;
            cfg.animHandle.Play(bb.animator, EnemyConfig.EnemyAnims.Stunned);
            mutateCtx.isStunned = true;
            bb.rb.linearVelocity = new Vector2(0, bb.rb.linearVelocity.y);
            bb.damageFlasher.Flash(DamageFlasher.FlashType.Stun);
            bb.enemiesSoundConfig.Play(bb.audioSource, EnemiesSoundConfig.EnemiesSounds.Stunned);
            facade.Actions.ToggleHyperArmor.Publish(false);
            
            foreach (var abc in bb.attackingBoundsCheckers)
                abc.gameObject.SetActive(false);

            bb.fwdAttackTimer.Pause();
            
            facade.Actions.AddBodyColliderExclusionLayer.Publish("Enemy");
        }

        public void MutateUsingNewSetValues()
        {
            if (facade.FSM.CurrentStateType != typeof(Blocking))
                mutateCtx.isStunned = false;
            else 
                mutateCtx.isStunned = SetContext.isStunned;
        }

        public void OnExitState(IEnemyContextView ctx)
        {
            mutateCtx.decidedToBlock = false;
            mutateCtx.hyperArmorWindowActive = false;
            bb.blockWaitTimer.StartAndReset();
            
            bb.fwdAttackTimer.Resume();
            bb.attackingBoundsCheckers[bodyAttkColliderIndex].gameObject.SetActive(true);

            facade.GetFunctionality<IAPI_ResetExclusionLayers>().InvokeAndSendDepdancies(new ResetBodyExclusionLayers());
        }
    }
    
    class DyingState : UnboundFunctionality<EnemyController, IEnemyContextView>,
        FSM_STATE_ENTER<IEnemyContextView>
    {
        EnemyConfig cfg => facade.API_Config<EnemyConfig>(); EnemyBlackboard bb => facade.API_Blackboard<EnemyBlackboard>(); EnemyContextData mutateCtx => facade.API_Context<EnemyContextData>();
        public DyingState(EnemyController facade) : base(facade) { }

        protected override void Awake()
        {
            bb.dyingStateTimer = new CountdownTimer(cfg.dyingState.dyingStatePeriod);
            facade.InitTimer(bb.dyingStateTimer, true);
            bb.dyingStateTimer.OnTimerStop.Add(StopDyingAfterCertainTime);
        }

        public void OnEnterState(IEnemyContextView ctx)
        {
            Debug.Log("Enemy Now Dying");
            cfg.animHandle.PlayWeightSet(bb.animator, EnemyConfig.EnemyAnims.Dying, 1);
            bb.dyingStateTimer.StartAndReset();
            bb.viewRange.enabled = false;
            facade.Actions.AddBodyColliderExclusionLayer.Publish("Enemy");
        }

        void StopDyingAfterCertainTime()
        {
            if(bb.livingEntity.isDead) return;
            Debug.Log("Stopped Dying");
            bb.animator.SetLayerWeight(cfg.animHandle.GetLayer(EnemyConfig.EnemyAnims.Dying), 0);
            bb.livingEntity.Heal(cfg.dyingState.outOfDeathStateHealAmount, out var newState);
            mutateCtx.currentHealthState = newState;
            bb.viewRange.enabled = true;
            mutateCtx.isBeingFinished = false;
            facade.GetFunctionality<IAPI_ResetExclusionLayers>().InvokeAndSendDepdancies(new ResetBodyExclusionLayers());
        }
    }
    

    class FaceDir : UnboundFunctionality<EnemyController, IEnemyContextView>,
        UPDATE<IEnemyContextView>
    {
        EnemyConfig cfg => facade.API_Config<EnemyConfig>(); EnemyBlackboard bb => facade.API_Blackboard<EnemyBlackboard>(); EnemyContextData mutateCtx => facade.API_Context<EnemyContextData>();
        public FaceDir(EnemyController facade) : base(facade) { }

        public override PipelineBuilder<IEnemyContextView> InjectSteps(PipelineBuilder<IEnemyContextView> builder) =>
            builder
                .Add_ShortCircuit(new FuncCtxPredicate<IEnemyContextView>(ctx => !ctx.canSeeTarget))
                .Add_ShortCircuit(new FuncCtxPredicate<IEnemyContextView>(ctx => ctx.yelling))
                .Add_ShortCircuit(new FuncPredicate(() => facade.FSM.CurrentStateType == typeof(ForwardAttack)));
        

        public override void Execute(IEnemyContextView ctx)
        {
            float targX = bb.target.position.x;
            float myX = facade.transform.position.x;
            float dif = targX - myX;
            if (dif < 0)
            {
                bb.faceDirTransform.rotation = Quaternion.Euler(0, 0, 0);
                mutateCtx.facingDirection = IEntityCtx.FaceDirection.Left;
            }
            else
            {
                bb.faceDirTransform.rotation = Quaternion.Euler(0, 180, 0);
                mutateCtx.facingDirection = IEntityCtx.FaceDirection.Right;
            }
            //Debug.Log("facing");
        }
    }
    
    class Idle : UnboundFunctionality<EnemyController, IEnemyContextView>,
        FSM_STATE_ENTER<IEnemyContextView>,
        FSM_STATE_EXIT<IEnemyContextView>
    {
        EnemyConfig cfg => facade.API_Config<EnemyConfig>(); EnemyBlackboard bb => facade.API_Blackboard<EnemyBlackboard>(); EnemyContextData mutateCtx => facade.API_Context<EnemyContextData>();
        public Idle(EnemyController facade) : base(facade) { }

        public override PipelineBuilder<IEnemyContextView> InjectSteps(PipelineBuilder<IEnemyContextView> builder) => builder
                .Add_ShortCircuit(new FuncCtxPredicate<IEnemyContextView>(ctx => ctx.canSeeTarget));

        public void OnEnterState(IEnemyContextView ctx)
        {
            Debug.Log("Entered Idle State");
            cfg.animHandle.Play(bb.animator, EnemyConfig.EnemyAnims.Idle);
            facade.Actions.AddBodyColliderExclusionLayer.Publish("Enemy");
        }

        public void OnExitState(IEnemyContextView ctx)
        {
            facade.GetFunctionality<IAPI_ResetExclusionLayers>().InvokeAndSendDepdancies(new ResetBodyExclusionLayers());
        }
    }


    class ViewRange : BoundSetFunctionality<EnemyController, IEnemyContextView, ViewRange.Setter>, 
        ON_SET
    {
        EnemyConfig cfg => facade.API_Config<EnemyConfig>(); EnemyBlackboard bb => facade.API_Blackboard<EnemyBlackboard>(); EnemyContextData mutateCtx => facade.API_Context<EnemyContextData>();
        public class Setter : DataSetter<bool>
        {
            [ShowInInspector] public bool canSeeTarget => Get;
        }

        protected override void Awake()
        {
            mutateCtx.canSeeTarget = new DelayBuffer<bool>(false, cfg.viewRange.delayToStopChasing);
        }

        public ViewRange(IPublisher publisher, EnemyController facade) : base(publisher, facade) { }

        public void MutateUsingNewSetValues()
        {
            Debug.Log("View Range: NEW " + SetContext.canSeeTarget);
            if(!SetContext.canSeeTarget)
                mutateCtx.canSeeTarget.SetBuffered(SetContext.canSeeTarget);
            else 
                mutateCtx.canSeeTarget.SetNotBuffered(SetContext.canSeeTarget);
        }
    }


    class Jump : UnboundFunctionality<EnemyController, IEnemyContextView>,
        FIXED_UPDATE<IEnemyContextView>
    {
        EnemyConfig cfg => facade.API_Config<EnemyConfig>(); EnemyBlackboard bb => facade.API_Blackboard<EnemyBlackboard>(); EnemyContextData mutateCtx => facade.API_Context<EnemyContextData>();
        public Jump(EnemyController facade) : base(facade) { }

        protected override void Awake()
        {
            bb.jumpTimer = new CountdownTimer(cfg.jump.jumpDelay);
            facade.InitTimer(bb.jumpTimer, true);
        }

        public override PipelineBuilder<IEnemyContextView> InjectSteps(PipelineBuilder<IEnemyContextView> builder) => builder
                .Add_ShortCircuit(new FuncPredicate(() => !cfg.jump.canJump))
                .Add_ShortCircuit(new FuncCtxPredicate<IEnemyContextView>(ctx => !ctx.canSeeTarget))
                .Add_ShortCircuit(new FuncPredicate(() => facade.FSM.CurrentStateType == typeof(DyingState)))
                .Add_ShortCircuit(new FuncCtxPredicate<IEnemyContextView>(ctx => !ctx.travelAngleTooCloseToVertical))
                .Add_ShortCircuit(new FuncPredicate(() => bb.jumpTimer.isRunning));
        
        
        public override void Execute(IEnemyContextView ctx)
        {
            bb.jumpTimer.StartAndReset();
            bb.rb.AddForce(Vector2.up * cfg.jump.jumpForceScalar, ForceMode2D.Impulse);
            Debug.Log("ENEMY Jump");
        }
    }
    
    
    class InAir : UnboundFunctionality<EnemyController, IEnemyContextView>,
        FIXED_UPDATE<IEnemyContextView>
    {
        EnemyConfig cfg => facade.Config; EnemyBlackboard bb => facade.API_Blackboard<EnemyBlackboard>();
        public InAir(EnemyController facade) : base(facade) { }
        public override void Execute(IEnemyContextView ctx)
        {
            facade.API_Context<EnemyContextData>().isGrounded = IsGrounded();
            if(!ctx.isGrounded) bb.rb.AddForce(-facade.transform.up * cfg.inAir.fallScalar, cfg.inAir.forceMode);
        }

        bool IsGrounded()
        {
            for (int i = 0; i < bb.feetPoints.Length; i++)
                if (Physics2D.Raycast(bb.feetPoints[i].position, -facade.transform.up, cfg.inAir.checkDist, cfg.inAir.mask)) return true;
            return false;
        }
    }

    class Follow : UnboundFunctionality<EnemyController, IEnemyContextView>,
        FIXED_UPDATE<IEnemyContextView>,
        FSM_STATE_ENTER<IEnemyContextView>,
        FSM_STATE_EXIT<IEnemyContextView>
    {
        EnemyConfig cfg => facade.API_Config<EnemyConfig>(); EnemyBlackboard bb => facade.API_Blackboard<EnemyBlackboard>();
        EnemyContextData mutateCtx => facade.API_Context<EnemyContextData>();
        public Follow(EnemyController facade) : base(facade) { }

        protected override void Awake()
        {
            PersistentAction computePath = new PersistentAction(() => bb.seeker.StartPath(bb.rb.position, bb.target.position, OnPathComplete));
            bb.computePath.InjectMethod(computePath);
            
            
            
        }
        
        public override PipelineBuilder<IEnemyContextView> InjectSteps(PipelineBuilder<IEnemyContextView> builder) => builder
                .Add_ShortCircuit(new FuncCtxPredicate<IEnemyContextView>(ctx => !ctx.canSeeTarget))
                .Add_ShortCircuit(new FuncPredicate(() => facade.FSM.CurrentStateType == typeof(Hooked)))
                .Add_ShortCircuit(new FuncPredicate(() => facade.FSM.CurrentStateType == typeof(DyingState)))
                .Add_ShortCircuit(new FuncPredicate(() => facade.FSM.CurrentStateType == typeof(Stunnable)))
                .Add_ShortCircuit(new FuncPredicate(() => facade.FSM.CurrentStateType == typeof(Blocking)))
                .Add_ShortCircuit(new FuncPredicate(() => facade.FSM.CurrentStateType == typeof(Yell) || mutateCtx.yelling))
                .Add_Middleware(_ => bb.computePath.RateLimitedUpdateTick())
                .Add_ShortCircuit(new FuncCtxPredicate<IEnemyContextView>(ctx => ctx.path == null))
                .Add_Middleware(GrabVariables)
                .Add_ShortCircuit(new FuncCtxPredicate<IEnemyContextView>(ReachedEndOfPath))
                .Add_ShortCircuit(new FuncCtxPredicate<IEnemyContextView>(TravelAngleTooCloseToVertical));

        
        public override void Execute(IEnemyContextView ctx)
        {
            //Debug.Log("Follow");
            if (ctx.distToNextWaypoint < cfg.follow.nextWaypointDistance)
                if (ctx.currentWaypointIndex + 1 < ctx.path.vectorPath.Count) mutateCtx.currentWaypointIndex++;
            
            bb.rb.AddForce(ctx.force);
            bb.enemiesSoundConfig.Play(bb.audioSource, EnemiesSoundConfig.EnemiesSounds.Walk, true);
        }

        void GrabVariables(IEnemyContextView ctx)
        {
            mutateCtx.pos = bb.rb.position; 
            mutateCtx.nextWaypoint = ctx.path.vectorPath[ctx.currentWaypointIndex];
            mutateCtx.dirToNextWaypoint = (ctx.nextWaypoint - ctx.pos).normalized;
            mutateCtx.force = ctx.dirToNextWaypoint * (cfg.follow.speedScalar * Time.deltaTime);
            mutateCtx.distToNextWaypoint = Vector2.Distance(ctx.pos, ctx.nextWaypoint);
            mutateCtx.distToEndNode = Mathf.Abs((bb.rb.position - (Vector2)ctx.path.vectorPath[^1]) .magnitude);
        }
        
        bool TravelAngleTooCloseToVertical(IEnemyContextView ctx)
        {
            float dot = Vector2.Dot(ctx.dirToNextWaypoint.normalized, Vector2.up); // dot: 1 = up, 0 = horizontal, -1 = down
            float angleToUp = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;
            //Debug.Log($"angleToUp: {angleToUp} | dot: {dot}");
            if (dot < 0f) return false;  // If moving downward, always allow
            float threshold = Mathf.Cos(cfg.follow.minHorizAngleToFollow * Mathf.Deg2Rad); // Only restrict upward movement
            bool isTooVertical = dot > threshold;
            if(isTooVertical)
                bb.enemiesSoundConfig.soundHandle.StopIfLooping(bb.audioSource, EnemiesSoundConfig.EnemiesSounds.Walk);
            return mutateCtx.travelAngleTooCloseToVertical = isTooVertical;
        }


        bool ReachedEndOfPath(IEnemyContextView ctx)
        {
            if (ctx.distToEndNode < cfg.follow.stoppingDistToTarget)
                { 
                    mutateCtx.reachedEndOfPath = true;
                    bb.enemiesSoundConfig.soundHandle.StopIfLooping(bb.audioSource, EnemiesSoundConfig.EnemiesSounds.Walk);
                    return true;
                    
                }
            else
                { mutateCtx.reachedEndOfPath = false; return false; }
        }

        void OnPathComplete(Path path)
        {
            if (path.error) return;
            mutateCtx.path = path;
            mutateCtx.currentWaypointIndex = 0;
        }

        public void OnEnterState(IEnemyContextView ctx)
        {
            Debug.Log("Entered Follow State");
            cfg.animHandle.Play(bb.animator, EnemyConfig.EnemyAnims.Walk);
        }

        public void OnExitState(IEnemyContextView ctx)
        {
            bb.enemiesSoundConfig.soundHandle.StopIfLooping(bb.audioSource, EnemiesSoundConfig.EnemiesSounds.Walk);
        }
    }
    
    
}