using System;
using System.Collections;
using System.Collections.Generic;
using EMILtools.Core;
using EMILtools.Extensions;
using EMILtools.Systems;
using EMILtools.Timers;
using Sirenix.OdinInspector;
using UnityEngine;

public class Hook : MonoBehaviour, TimerUtility.ITimerUser
{
    public struct FinisherContext
    {
        public bool finisherActive;
        public CountdownTimer finisherTimer;
        public PersistentAction HookedBreakout;
        public FinisherEvent finisherEvent;
        public Ref<bool> finisherInputAvaliable;
        public IDamageable damageable;

        public FinisherContext(bool finisherActive, CountdownTimer finisherTimer, PersistentAction hookedBreakout, Ref<bool> finisherInputAvaliable, IDamageable damageable, FinisherEvent finisherEvent = null)
        {
            this.finisherActive = finisherActive;
            this.finisherTimer = finisherTimer;
            HookedBreakout = hookedBreakout;
            this.finisherInputAvaliable = finisherInputAvaliable;
            this.damageable = damageable;
            this.finisherEvent = finisherEvent;
        }
    }

    public enum HookPhases
    {
        Held,
        CastingOut,
        HitMaxDistAndFalling,
        HookedToSomething,
        HookAttacking,
        HookSnapping,
        Drooping
    }
    public bool withinRange;
    public HookPhases phase = HookPhases.Held;
    public LayerMask worldMask;
    public LayerMask targetMask;
    public Rigidbody2D rb;
    
    public Transform rodParent;
    public Collider2D hookCollider;
    public DistanceJoint2D joint;
    public GameObject breakerCollider;

    public AnimationCurve lineVerticalOffsetCurve;
    public float verticalOffsetScalar = 3f;
    public LineRenderer line;
    public Transform hookVisual;
    public GameObject rodVisual;

    public float maxDistanceToAutoRecal = 8f;
    public float maxDistanceToBreak = 12f;
    public int linePoints = 10;
    public float lineLerpSpeed = 10f;
    public float dist;
    public Ref<float> remainingCurveWhenAtMaxDist = 0.5f;

    public CountdownTimer lineDroopTimer;
    public Ref<float> lineDroopTime = 0.5f;
    
    public CountdownTimer snapToHookedTimer;
    public Ref<float> snapTimeWhenHooked = 0.2f;
    
    public CountdownTimer hookAttackTimer;
    public Ref<float> hookAttackTimerSpeed = 0.2f;
    
    public CountdownTimer hookAttackSnapTimer;
    public Ref<float> hookAttackSnapSpeed = 0.2f;

    public AnimationCurve hookAttackCurve;

    Gradient cachedLineColor;
    
    [ColorUsage(true, true)]
    public Color breakingColor;
    
    Action attachedSignal;
    Action hookAttackFinished;

    void Awake()
    {
        cachedLineColor = line.colorGradient;
        line.positionCount = linePoints;
        lineDroopTimer = new CountdownTimer(lineDroopTime, true);
        snapToHookedTimer = new CountdownTimer(snapTimeWhenHooked, true);
        hookAttackSnapTimer = new CountdownTimer(hookAttackSnapSpeed);
        hookAttackTimer = new CountdownTimer(hookAttackTimerSpeed);
        
        hookAttackTimer.OnTimerStop.Add(() => {
            phase = HookPhases.HookSnapping;
        });
        hookAttackSnapTimer.OnTimerStop.Add(() => {
            hookAttackFinished?.Invoke();
            phase = HookPhases.Held;
        });
        
        this.InitTimer(lineDroopTimer, true);
        this.InitTimer(snapToHookedTimer, true);
        this.InitTimer(hookAttackTimer, true);
        this.InitTimer(hookAttackSnapTimer, true);
    }

    private void Start()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        joint.enabled = false;

        ResetHook();
    }

    void Update()
    {
        dist = Vector3.Distance(transform.position, rodParent.position);
        withinRange = dist < maxDistanceToAutoRecal;
        
        int count = line.positionCount;
        float step = dist / (count - 1);
        Vector3 dir = (transform.position - rodParent.position);
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = (line.transform.InverseTransformPoint(line.transform.position + dir * (step * i))) / dist;
            
            
            switch (phase)
            {
                case HookPhases.Held: 
                    line.SetPosition(i, rodParent.position); 
                    transform.position = rodParent.position;

                    continue;
                case HookPhases.CastingOut: CastOut(); break;
                case HookPhases.HitMaxDistAndFalling: HitMaxDistAndFalling(); break;
                case HookPhases.HookedToSomething: HookedOntoSomething(); break;
                case HookPhases.HookAttacking: HookAttacking(); break;
                case HookPhases.HookSnapping: HookSnapping(); break;
            }


            void CastOut()
            {
                line.colorGradient = cachedLineColor;
                
                float point = (float)i / (count - 1); 
                float curvedScalar = verticalOffsetScalar * lineVerticalOffsetCurve.Evaluate(point);
                
                float distProg = dist.ProgressWhenApproaching(0f, maxDistanceToAutoRecal, true);
                float distanceResponsiveScalar = curvedScalar * ((1f + remainingCurveWhenAtMaxDist) - distProg);

                // Dont apply offset to end points
                float nonEndPointsOffset = (i != 0 && i != count - 1) ? distanceResponsiveScalar : 1f;

                
                pos = pos.With(y: pos.y + nonEndPointsOffset);
               // Debug.Log($"Curved: {distanceResponsiveScalar} Regular: {nonEndPointsOffset}, Dist Prog : {distProg}");
                line.SetPosition(i, pos);
                
                
                hookVisual.transform.rotation = Quaternion.LookRotation(Vector3.forward, rodParent.position - transform.position);
            }
            
            void HitMaxDistAndFalling()
            {
                if(!lineDroopTimer.isRunning) lineDroopTimer.StartAndReset();

                float remainingCurveWhenAtMaxDist_Falling = remainingCurveWhenAtMaxDist * (lineDroopTimer.Progress);
                
                float point = (float)i / (count - 1); 
                float curvedScalar = verticalOffsetScalar * lineVerticalOffsetCurve.Evaluate(point);
                
                float distProg = dist.ProgressWhenApproaching(0f, maxDistanceToAutoRecal, true);
                float distanceResponsiveScalar = curvedScalar * ((1f + remainingCurveWhenAtMaxDist_Falling) - distProg);

                // Dont apply offset to end points
                float nonEndPointsOffset = (i != 0 && i != count - 1) ? distanceResponsiveScalar : 1f;

                
                pos = pos.With(y: pos.y + nonEndPointsOffset);
                //Debug.Log($"Curved: {distanceResponsiveScalar} Regular: {nonEndPointsOffset}, Dist Prog : {distProg}");
                line.SetPosition(i, pos);
            }

            void HookedOntoSomething()
            {
                if(!snapToHookedTimer.isRunning) snapToHookedTimer.StartAndReset();
                
                //Debug.Log("snap prog : " + (1-snapToHookedTimer.Progress));
                
                Vector2 oldPos = line.GetPosition(i);
                Vector2 newPos = pos;
                Vector2 lerpPos = Vector2.Lerp(oldPos, newPos, (1- snapToHookedTimer.Progress));
                
                line.SetPosition(i, lerpPos);

                if (dist > maxDistanceToBreak)
                {
                    ResetHook();
                    hookAttackFinished?.Invoke();
                }
                else
                {
                    float lerpPoint = Mathf.InverseLerp(maxDistanceToAutoRecal, maxDistanceToBreak, dist);

                    Color newColor = Color.Lerp(
                        cachedLineColor.Evaluate(lerpPoint),
                        breakingColor,
                        lerpPoint
                    );
                    
                    line.startColor = newColor;
                    line.endColor = newColor;
                }
            }
            
            void HookAttacking()
            {
                if (!hookAttackTimer.isRunning)
                    hookAttackTimer.StartAndReset();

                float progress = Mathf.Clamp01(1- hookAttackTimer.Progress);
                float pointT = (linePoints <= 1) ? 0f : (float)i / (linePoints - 1);

                if (pointT <= progress)
                {
                    // 0..1 along only the traversed part
                    float localT = (progress > 0f) ? pointT / progress : 0f;

                    // continuous pattern 
                    float yOffset = hookAttackCurve.Evaluate(localT);
                    pos = pos.With(y: pos.y + yOffset);
                }

                line.SetPosition(i, pos);
            }

            void HookSnapping()
            {
                if(!hookAttackSnapTimer.isRunning) hookAttackSnapTimer.StartAndReset();
                
                //Debug.Log("snap prog : " + (1-snapToHookedTimer.Progress));
                
                Vector2 oldPos = line.GetPosition(i);
                Vector2 newPos = pos;
                Vector2 lerpPos = Vector2.Lerp(oldPos, newPos, (1- hookAttackSnapTimer.Progress));
                
                line.SetPosition(i, lerpPos); 
            }
            
        }
    }

    bool InMask(Collision2D other, LayerMask mask) => (mask.value & (1 << other.gameObject.layer)) != 0;
    
    private void OnCollisionEnter2D(Collision2D other)
    {
        if(InMask(other, worldMask)) StopHookMovement();
        if(InMask(other, targetMask)) AttachHook(other.transform);
    }

    [Button]
    public void HookAttack()
    {
        hookAttackTimer.StartAndReset();
        phase = HookPhases.HookAttacking;
        breakerCollider.SetActive(true);
    }

    public void CastHook(Vector2 mousePos, float forceScalar)
    {
        hookCollider.enabled = true;
        joint.enabled = false;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.ResetVel2D();
        // as mouse pos approaches 0 from left and right, make the dir go higher
            float addY = (mousePos - (Vector2)rodParent.position).magnitude * 0.13f;
            mousePos = mousePos.With(y: mousePos.y + addY);
        Vector3 worldDir = (mousePos - (Vector2)rodParent.position).normalized;
        Vector2 dir = worldDir * forceScalar;       
        //Debug.Log("[CH] mousePos: " +  mousePos + ", inv Trans: " + rodParent.InverseTransformDirection(mousePos).normalized + " dist: " + addY);

        
        breakerCollider.SetActive(false);
        rb.AddForce(dir, ForceMode2D.Impulse);
        transform.position = rodParent.position;
        transform.SetParent(null);
        phase = HookPhases.CastingOut;
        lineDroopTimer.canRun = true;
        snapToHookedTimer.canRun = true;
        if(gameObject.activeInHierarchy)
            StartCoroutine(C_CheckIfHookDistanceIsTooFar());
    }

    public void SetupHookAttackFinishedSignal(Action signal) => hookAttackFinished = signal;

    public bool ResetHook()
    {
        if(phase == HookPhases.HookAttacking) return false;
        breakerCollider.SetActive(false);
        transform.SetParent(rodParent);
        transform.position = rodParent.position;
        rb.bodyType = RigidbodyType2D.Kinematic;
        joint.enabled = false;
        phase = HookPhases.Held;
        withinRange = false;
        var defaultHookRot = Quaternion.Euler(new Vector3(0, 180, 70));
        hookVisual.localRotation = defaultHookRot;
        for (int i = 0; i < linePoints; i++) line.SetPosition(i, rodParent.position);
        return true;
    }

    void StopHookMovement()
    {
        Debug.Log("HIT GROUND");
        rb.bodyType = RigidbodyType2D.Kinematic;
        hookCollider.enabled = false;
        rb.ResetVel2D(); 
        phase = HookPhases.HookedToSomething;
        if(!lineDroopTimer.isRunning && phase != HookPhases.Drooping) lineDroopTimer.StartAndReset();
    }

    void AttachHook(Transform parent)
    {
        Debug.Log("HOOK ATTACHING");
        rb.bodyType = RigidbodyType2D.Kinematic;
        transform.SetParent(parent);
        rb.ResetVel2D();
        attachedSignal?.Invoke();
        hookCollider.enabled = false;
        phase = HookPhases.HookedToSomething;
        if(!lineDroopTimer.isRunning && phase != HookPhases.Drooping) lineDroopTimer.StartAndReset();
    }

    void HookReachedMaxDist()
    {
        joint.enabled = true;
        phase = HookPhases.HitMaxDistAndFalling;
        joint.distance = Vector3.Distance(transform.position, rodParent.position);
    }

    IEnumerator C_CheckIfHookDistanceIsTooFar()
    {
        while (phase == HookPhases.CastingOut)
        {
            yield return null;
            dist = Vector3.Distance(transform.position, rodParent.position);
            if (!(dist > maxDistanceToAutoRecal)) continue;
            HookReachedMaxDist();
            yield break;

        }
    }

    void RecallHook()
    {
        Debug.Log("HOOK RECALLED");
    }
    
    public void InjectAttachedSignal(Action signal) => attachedSignal = signal;
}
