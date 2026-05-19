using System;
using EMILtools.Core;
using EMILtools.Extensions;
using EMILtools.Systems;
using EMILtools.Timers;
using UnityEngine;
using Sirenix.OdinInspector;
using static AttackingBoundsChecker;
using static PrimaryInputAuthority;
using static EMILtools.Systems.IInputSubordinate<PlayerController.PlayerInputMap,PrimaryInputAuthority.Subordinates>;
using static HookCollisionChecker;


public class PlayerController : MonoFacade<
        PlayerFunctionality,
        PlayerConfig,
        PlayerStructure,
        PlayerController.ActionMap>,
    IInputSubordinate<PlayerController.PlayerInputMap, Subordinates>,
    IBoundsCheckMsgReceiver<Collider2D, AttackCtx>,
    ICollisionCheckMsgReceiver<Collider2D, HookContext>,
    ISignalReceiverTC<bool>,

    IEntityFacade
{
    Transform IFacade.transform => gameObject.transform;

    public class ActionMap : IActionMap
    {
        public readonly Publisher<IPlayerContextView> Pogo = new();
        public readonly Publisher<AttackCtx> TakeDamage = new();
        public readonly Publisher<IPlayerContextView> IvunrabilityVisualization = new();
        public readonly Publisher<IPlayerContextView> HookAttack = new();
        public readonly Publisher<bool> AttackColliderSetActive = new();
        public readonly Publisher<bool> Land = new();

        public readonly Publisher<Hook.FinisherContext> Finisher = new();

        public IContextViewImmutable ctx { get; }
    }

    public class PlayerInputMap : InputMap
    {
        public readonly Publisher<(bool, float)> Move = new();
        public readonly Publisher<bool> Jump = new();
        public readonly Publisher<Vector2> Look = new();
        public readonly Publisher<bool> Attack = new();
        public readonly Publisher<bool> Hook = new();
        public readonly Publisher<bool> FinishInput = new();
    }

    public PlayerInputMap Input { get; set; }

    [field: SerializeField]
    [field: PropertyOrder(-1)]
    public SubordinateContext inputSubordinateContext { get; set; }

    public PlayerInputMap InjectInputMap() => new PlayerInputMap();
    public void InitSubordinate() => InitializeFacade();
    public void OnAuthorityReceived() => Functionality.Bind();
    public void OnAuthorityLost() => Functionality.Unbind();


    public void OnEnterBounds(Collider2D collidedWith, BoundsChecker<AttackCtx> sender, AttackCtx ctx)
    {
        if (ctx.attackingColliderTag == "PlayerDownPogo")
        {
            Actions.Pogo.Publish(API_Context<PlayerContextData>());
            return;
        }
        Debug.Log($"Player took damage: {ctx.damageInfo.dmg}");
        Actions.TakeDamage.Publish(ctx);
    }

    public void OnStayBounds(Collider2D collidedWith, BoundsChecker<AttackCtx> sender, AttackCtx ctx)
    {
        Debug.Log($"Player try receive damage: {ctx.damageInfo.dmg}");
        Actions.TakeDamage.Publish(ctx);
    }

    /// <summary>
    /// When the player hooks onto a target
    /// </summary>
    /// <param name="collidedWith"></param>
    /// <param name="sender"></param>
    /// <param name="ctx"></param>
    public void OnEnterBounds(Collider2D collidedWith, CollisionChecker<HookContext> sender, HookContext ctx)
    {
        if (collidedWith == null)
        {
            ResetHook();
            return;
        }
        if (collidedWith.Has<LivingEntity>(out var livingEntity) && livingEntity.isDead)
        {
            ResetHook();
            return;
        }
        Debug.Log("3");
        if (!collidedWith.TryGetComponent<EnemyController>(out var enemyCont))
        {
            ResetHook();
            return;
        }
        var actionMap = enemyCont.API_Actions<EnemyController.ActionMap>();
        API_Context<PlayerContextData>().targetStunPublisher = actionMap.Stun;
        API_Context<PlayerContextData>().isHookLatchedOntoTarget = true;
        API_Context<PlayerContextData>().targetIsHookedBySomething = actionMap.isHookedBySomething;
        API_Context<PlayerContextData>().targetIsHookedBySomething.Publish((true, Actions.Finisher));
        void ResetHook()
        {
            API_Blackboard<PlayerBlackboard>().hook.ResetHook();
            API_Context<PlayerContextData>().isHooking = false;
        }
    }
    
    public void ReceiveSignal(string tag, bool ctx)
    {
        if (tag != "ATTACKANIM") return;
        Actions.AttackColliderSetActive.Publish(ctx);
    }
}

































