using System;
using System.Collections.Generic;
using EMILtools.Core;
using UnityEngine;
using EMILtools.Systems;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "EMILtools/ScriptableObjects/Configs/Player")]
public class PlayerConfig : Config, IEntityConfig
{
    public enum MouseZones { LeftScreen, RightScreen }
    public enum PlayerAnimations { Idle, Move, JumpStart, JumpInAir, Land, AttackFront, AttackUp, AttackDown, 
        Cast, Casting, CastPull, CastBreakoff }
    public enum PlayerBlendVars { }

    
    [SerializeField] public Move move;
    [SerializeField] public Jump jump;
    [SerializeField] public Friction friction;
    [SerializeField] public Fall fall;
    [SerializeField] public Facing facing;
    [SerializeField] public AnimHandle<PlayerAnimations, PlayerBlendVars> animHandle;
    [SerializeField] public Hook hook;
    [SerializeField] public Pogo pogo;
    [field: PropertyOrder(-1)] [field: SerializeField] public IEntityConfig.ClampLateralMov clampLateralMove { get; set; }
    [field: PropertyOrder(-1)] [field: SerializeField] public IEntityConfig.HitStop hitStop { get; set; }
    [field: PropertyOrder(-1)] [field: SerializeField] public IEntityConfig.TakeDmg takeDmg { get; set; }


    [Serializable]
    public struct Pogo
    {
        [field: SerializeField] public float pogoForceStrength { get; private set; }
    }


    [Serializable]
    public struct Hook
    {
        [field: SerializeField] public float hookForce { get; private set; }
    }
    
    [Serializable]
    public struct Facing
    {
        [field: SerializeField] public IEntityCtx.FaceDirection faceDirection { get; private set; }
        [field: SerializeField] public RectLabeled<MouseZones>[] callbackZones { get; private set; }
    }
    
    [Serializable]
    public struct Fall
    {
        [field: SerializeField] public float checkDist;
        [field: SerializeField] public LayerMask mask;
        
        [field: SerializeField] public ForceMode2D forceMode;
        [field: SerializeField] public float scalar;
        [field: SerializeField] public Ref<float> fallingBufferWindow;
    }
    
    [Serializable]
    public struct Friction
    {
        [field: SerializeField] public float frictionScalar { get; private set; }
    }

    [Serializable]
    public struct Jump
    {
        [field: SerializeField] public float jumpCurveRate { get; private set; }
        [field: SerializeField] public int maxJumps { get; private set; }
        [field: SerializeField] public ForceMode2D forceMode;
        [field: SerializeField] public float scalar;
        [field: SerializeField] public Ref<float> coyoteInputWindow;
        [field: SerializeField] public float landTime;
    }
    
    [Serializable]
    public struct Move
    {
        [field: SerializeField] public float speedScalar { get; private set; }
        [field: SerializeField] public ForceMode2D forceMode2d { get; private set; }
        [field: SerializeField] public float maxVelocity { get; private set; }
        [field: SerializeField] public float acceleration { get; private set; }
        [field: SerializeField] public float deceleration { get; private set; }

    }
}