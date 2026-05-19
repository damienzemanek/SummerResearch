using System;
using EMILtools_Private.Core;
using UnityEngine;
using EMILtools.Systems;
using static AttackingBoundsChecker;
using static CanSeeBoundsChecker;
using static EnemyController;

public class EnemyController : MonoFacade<
    EnemyFunctionality,
    EnemyConfig,
    EnemyStructure,
    ActionMap>,
        IBoundsCheckMsgReceiver<Collider2D, CanSeeContext>, 
        IBoundsCheckMsgReceiver<Collider2D, AttackCtx>,
        ISignalReceiverTC<(bool, FinisherChoice)>,
        ISignalReceiverTC<BoolInt>,
        ISignalReceiverT<LivingEntity.PhasedHealthThresholdEnum>,
        IBoundsCheckMsgReceiver<Collider2D, InRangeBoundsChecker.InRangeContext>,
IEntityFacade
{
    
    
    Transform IFacade.transform => gameObject.transform;
    public class ActionMap : IActionMap
    {
        public readonly Publisher<string> AddBodyColliderExclusionLayer = new();
        public readonly Publisher<bool> Yell = new();
        public readonly Publisher<bool> AttackInRange = new();
        public readonly Publisher<bool> ToggleHyperArmor = new();
        public readonly Publisher Idle = new();
        public readonly Publisher<bool> CanSeeTarget = new ();
        public readonly Publisher<AttackCtx> TakeDamage = new();
        public readonly Publisher<bool> Stun = new();
        public readonly Publisher<(bool isHooked, Publisher<Hook.FinisherContext>)> isHookedBySomething = new();
        public readonly Publisher<bool> FinisherInputAvaliable = new();
        

        public IContextViewImmutable ctx { get; }
    }
    protected void Awake() => InitializeFacade();
    void OnEnable() => Functionality.Bind();
    void OnDisable() => Functionality.Unbind();

    public void OnEnterBounds(Collider2D collidedWith, BoundsChecker<CanSeeContext> sender, CanSeeContext ctx) => CanSee(ctx.canSeeTarget);
    public void OnExitBounds(Collider2D collidedWith, BoundsChecker<CanSeeContext> sender, CanSeeContext ctx) => CanSee(ctx.canSeeTarget);
    void CanSee(bool canSee) => Actions?.CanSeeTarget.Publish(canSee).Forget("Can See");
    
    // Receive Attack DI
    public void OnEnterBounds(Collider2D collidedWith, BoundsChecker<AttackCtx> sender, AttackCtx ctx)
    {
        Debug.Log("DMG : to " + gameObject.name);
        Actions.TakeDamage.Publish(ctx);
    }
    
    
    // In Range DI
    public void OnStayBounds(Collider2D collidedWith, BoundsChecker<InRangeBoundsChecker.InRangeContext> sender, InRangeBoundsChecker.InRangeContext ctx)
    {
        if (ctx.inRange) Actions.AttackInRange.Publish(true);
    }

    public void OnExitBounds(Collider2D collidedWith, BoundsChecker<InRangeBoundsChecker.InRangeContext> sender,
        InRangeBoundsChecker.InRangeContext ctx) =>
        Actions.AttackInRange.Publish(false);


    public void ReceiveSignal(string tag, (bool, FinisherChoice) ctx)
    {
        if (tag == "FINISHER") Actions.FinisherInputAvaliable.Publish(ctx.Item1);
    }
    
    public void ReceiveSignal(string tag, BoolInt ctx)
    {
        Debug.Log("ANIM EVENT, BOOL INT: int: " + ctx.intVal + " bool: " + ctx.boolVal + " tag: " + tag + "");
        
        if (tag == "ATTACKANIM")
            API_Blackboard<EnemyBlackboard>().attackingBoundsCheckers[1].gameObject.SetActive(ctx.boolVal);
        
        if (tag == "YELL" && ctx.boolVal == false)
            Actions.Yell.Publish(false);

        if (tag == "FWDATTACK")
        {
            API_Blackboard<EnemyBlackboard>().attackingBoundsCheckers[1].gameObject.SetActive(ctx.boolVal);
            if(ctx.boolVal == false)
                API_Context<EnemyContextData>().decidedToFwdAttack = false;
        }
    }
    
    public bool phaseFourHit;
    public bool phaseThreeHit;
    public bool phaseTwoHit;
    public bool phaseOneHit;

    public void ReceiveSignal(LivingEntity.PhasedHealthThresholdEnum t)
    {
        Debug.Log("PHASE CHANGE STATE RECEIVED: " + t);
        
        switch (t)
        {
            case LivingEntity.PhasedHealthThresholdEnum.PhaseFour:
                
                if(phaseFourHit != true) Actions.Yell.Publish(true);
                phaseFourHit = true;
                
                break;
            case LivingEntity.PhasedHealthThresholdEnum.PhaseThree:
                
                if(phaseThreeHit != true) Actions.Yell.Publish(true);
                phaseThreeHit = true;
                
                
                break;
            case LivingEntity.PhasedHealthThresholdEnum.PhaseTwo: 
                
                if(phaseTwoHit != true) Actions.Yell.Publish(true);
                phaseTwoHit = true;
                
                API_Context<EnemyContextData>().hyperArmorUsableInState = true;
                
                EnemyFunctionality.EnemyDescisions CanNowBlock = new EnemyFunctionality.EnemyDescisions { blockingAllowed = true };
                GetFunctionality<EnemyFunctionality.IAPI_EnemyDescisionMaker>().InvokeAndSendDepdancies(CanNowBlock);
                
                break;
            case LivingEntity.PhasedHealthThresholdEnum.PhaseOne:
                
                if(phaseOneHit != true) Actions.Yell.Publish(true);
                phaseOneHit = true;
                
                API_Context<EnemyContextData>().forwardAttackUsableInState = true;
                
                EnemyFunctionality.EnemyDescisions CanNowFwdAttack = new EnemyFunctionality.EnemyDescisions { fwdAttackingAllowed = true };
                GetFunctionality<EnemyFunctionality.IAPI_EnemyDescisionMaker>().InvokeAndSendDepdancies(CanNowFwdAttack);
                
                break;
        }
    }
}












