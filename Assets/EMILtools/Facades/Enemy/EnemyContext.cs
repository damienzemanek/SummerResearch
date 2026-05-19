using System;
using EMILtools.Systems;
using EMILtools.Timers;
using Pathfinding;
using Sirenix.OdinInspector;
using UnityEngine;

public interface IEnemyContextView : IEntityCtx
{
    public bool reachedEndOfPath { get; }
    public bool isGrounded { get; }
    public bool isStunned { get; }
    public bool invulnerable { get; }
    public bool travelAngleTooCloseToVertical { get; }
    public bool isBeingFinished { get; }
    public Ref<bool> isFinisherInputAvaliable { get; }
    public DelayBuffer<bool> canSeeTarget { get; }
    public Vector2 pos { get; }
    public Vector2 nextWaypoint { get; }
    public Vector2 dirToNextWaypoint { get; }
    public Vector2 force { get; }
    public Path path { get; }
    public float distToNextWaypoint { get; }
    public float distToEndNode { get; }   
    public int currentWaypointIndex { get; }
    public bool hyperArmorWindowActive { get; }
    public bool yelling { get; }
    public bool decidedToBlock { get; }
    public bool decidedToFwdAttack { get; }
    public Enum currentHealthState { get; }
    public IEntityCtx.FaceDirection facingDirection { get; set; }
}

public class EnemyContextData : ContextData, IEnemyContextView
{
    [field: ShowInInspector] public bool reachedEndOfPath { get; set; }
    [field: ShowInInspector] public bool isGrounded { get; set; }
    [field: ShowInInspector] public bool travelAngleTooCloseToVertical { get; set; }
    [field: ShowInInspector] public bool isStunned { get; set; }
    [field: ShowInInspector] public bool isBeingFinished { get; set; }
    [field:ShowInInspector] public bool forwardAttackUsableInState { get; set; }
    [field:ShowInInspector] public bool hyperArmorUsableInState { get; set; }
    [field:ShowInInspector] public bool hyperArmorWindowActive { get; set; }
    [field:ShowInInspector] public bool decidedToBlock { get; set; }
    [field:ShowInInspector] public bool decidedToFwdAttack { get; set; }
    [field:ShowInInspector] public bool attacking { get; set; }
    [field:ShowInInspector] public bool yelling { get; set; }
    [field:ShowInInspector] public bool stunWindowActive { get; set; }
    [field: ShowInInspector] public DelayBuffer<bool> canSeeTarget { get; set; }
    [field: ShowInInspector] public bool invulnerable { get; set; }
    [field: ShowInInspector] public Ref<bool> isFinisherInputAvaliable { get; set; } = new(false);
    [field: ShowInInspector] public float hp { get; set; }
    [field: ShowInInspector] public Enum currentHealthState { get; set; }
    [field: ShowInInspector] public IEntityCtx.FaceDirection facingDirection { get; set; }

    [field: BoxGroup("A Star")] [field: ShowInInspector] public Path path { get; set; }
    [field: BoxGroup("A Star")] [field: ShowInInspector] public int currentWaypointIndex { get; set; }
    [field: BoxGroup("A Star")] [field: ShowInInspector] public Vector2 pos { get; set; }
    [field: BoxGroup("A Star")] [field: ShowInInspector] public Vector2 nextWaypoint { get; set; }
    [field: BoxGroup("A Star")] [field: ShowInInspector] public Vector2 dirToNextWaypoint { get; set; }
    [field: BoxGroup("A Star")] [field: ShowInInspector] public Vector2 force { get; set; }
    [field: BoxGroup("A Star")] [field: ShowInInspector] public float distToNextWaypoint { get; set; }
    [field: BoxGroup("A Star")] [field: ShowInInspector] public float distToEndNode { get; set; }
}