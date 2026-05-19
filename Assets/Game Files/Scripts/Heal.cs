using System;
using EMILtools.Timers;
using Sirenix.OdinInspector;
using UnityEngine;

public class Heal : MonoBehaviour, TimerUtility.ITimerUser
{
    [SerializeField] private LivingEntity target;
    [SerializeField] private float healAmount = 5f;
    [SerializeField] private float interval = 2f;

    private CountdownTimer healTimer;
    private bool isPlayerInside;

    private void Awake()
    {
        
        healTimer = new CountdownTimer(interval);
        this.InitTimer(healTimer, isFixed: false);
        
        healTimer.OnTimerStop.Add(ApplyHeal);
    }
    

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            if (target == null) target = other.GetComponent<LivingEntity>();
            
            if (target != null && !healTimer.isRunning)
            {
                healTimer.StartAndReset();
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            isPlayerInside = true;
            if (target == null) target = collision.collider.GetComponent<LivingEntity>();
            
            if (target != null && !healTimer.isRunning)
            {
                healTimer.StartAndReset();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            healTimer.Stop();
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            isPlayerInside = false;
            healTimer.Stop();
        }
    }

    private void ApplyHeal()
    {
        if (target != null && target.CompareTag("Player"))
        {
            target.Heal(healAmount, out Enum _);
        }
        
        if (isPlayerInside)
        {
            healTimer.StartAndReset();
        }
    }

    private void OnDestroy()
    {
        this.ShutdownTimers();
    }
}
