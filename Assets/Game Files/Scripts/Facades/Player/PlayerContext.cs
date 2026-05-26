using System;
using System.Collections.Generic;
using EMILtools.Core;
using EMILtools.Systems;
using EMILtools.Timers;
using Sirenix.OdinInspector;
using UnityEngine;

public interface IPlayerContextView : IEntityCtx
{
    public ICountdownTimer CoyoteTimer { get; }
    public ICurveValue JumpCurve { get; }
    public int jumps { get; }
    public bool IsGrounded { get; }
    public bool FallingWithoutJumpingFirst { get; }
    public bool isJumping { get; }
    public bool canJumpCoyote { get; }
    public bool landing { get; }
    public bool isAttacking { get; }
    public bool isMoving { get; }   
    bool isHooking { get; }
    bool isHookLatchedOntoTarget { get; }
    public bool isFinishing { get; }
    public Ref<bool> targetIsFinishInputAvaliable { get; }
    IDamageable currentFinishingTarget { get; }
    Vector2 mousePos { get; }
    Publisher<bool> targetStunPublisher { get; } 
    Publisher<(bool isHooked, Publisher<Hook.FinisherContext>)> targetIsHookedBySomething { get; }
    public FaceDirection facingDirection { get; }
    public PlayerBlackboard.AttackDir attackDir { get; }

}

[InlineProperty]
public class PlayerContextData : ContextData, IPlayerContextView
{
    [ShowInInspector] CountdownTimer coyoteTimer;
    [ShowInInspector] public ICurveValue JumpCurve => API_Blackboard<PlayerBlackboard>().jumpCurve;
    [ShowInInspector] public ReactiveIntercept<bool> isGrounded = new ReactiveIntercept<bool>();
    [ShowInInspector] public int jumps { get; set; }
    [ShowInInspector] public Vector2 mousePos { get; set; }
    [ShowInInspector] public DelayBuffer<bool> fallingWithoutJumpingFirst { get; set; }
    [ShowInInspector] public IEntityCtx.FaceDirection facingDirection { get; set; }
    [ShowInInspector] public PlayerBlackboard.AttackDir attackDir { get; set; }
    [ShowInInspector] public bool landing { get; set; }
    [ShowInInspector] public bool isJumping { get; set; }
    [ShowInInspector] public bool canJumpCoyote { get; set; } = true;
    [ShowInInspector] public bool isAttacking { get; set; }
    [ShowInInspector] public bool isMoving { get; set; }
    [ShowInInspector] public bool isHooking { get; set; }
    [ShowInInspector] public bool isHookLatchedOntoTarget { get; set; }
    [ShowInInspector] public bool isFinishing { get; set; }
    [ShowInInspector] public Publisher<bool> targetStunPublisher { get; set; }
    [ShowInInspector] public Ref<bool> targetIsFinishInputAvaliable { get; set; } = new(false);
    [ShowInInspector] public Publisher<(bool isHooked, Publisher<Hook.FinisherContext>)> targetIsHookedBySomething { get; set; }
    [ShowInInspector] public IDamageable currentFinishingTarget { get; set; }
    [ShowInInspector] public FinisherEvent currentEnemyFinisherEvent { get; set;}
    // API Distinct
    public ICountdownTimer CoyoteTimer => coyoteTimer;
    public bool IsGrounded => isGrounded;
    [field:ShowInInspector] public bool FallingWithoutJumpingFirst => fallingWithoutJumpingFirst;
    [field:ShowInInspector] public float hp { get; set; }
    [field:ShowInInspector] public bool invulnerable { get; set; }
    [field:ShowInInspector] public Enum currentHealthState { get; set; }
}