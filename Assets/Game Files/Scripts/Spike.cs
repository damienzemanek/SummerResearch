using System;
using EMILtools.Timers;
using Sirenix.OdinInspector;
using UnityEngine;

public class Spike : MonoBehaviour
{
    [SerializeField] private float damageAmount = 5f;
    [SerializeField] private float upwardForce = 10f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyDamage(other.gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            ApplyDamage(collision.gameObject);
        }
    }

    private void ApplyDamage(GameObject player)
    {
        Debug.Log("TEST 1");
        var livingEntity = player.GetComponent<LivingEntity>();
        if (livingEntity == null)
        {
            // Check if it's on a child or via Blackboard
            var blackboard = player.GetComponent<PlayerBlackboard>();
            if (blackboard != null) livingEntity = blackboard.livingEntity;
            else livingEntity = player.GetComponentInChildren<LivingEntity>();
        }
        Debug.Log("TEST 2");

        if (livingEntity != null)
        {
            livingEntity.TakeDamage(new IDamageable.DamageInfo((int)damageAmount, IDamageable.DamageType.Regular, IDamageable.DamageLocation.Body));
        }
        Debug.Log("TEST 3");

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            var blackboard = player.GetComponent<PlayerBlackboard>();
            if (blackboard != null) rb = blackboard.rb;
            else rb = player.GetComponentInChildren<Rigidbody2D>();
        }
        Debug.Log("TEST 4");

        if (rb != null)
        {
            // Reset velocity to ensure the "shoot up" effect is consistent
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.AddForce(Vector2.up * upwardForce, ForceMode2D.Impulse);
        }
    }

    private void OnDestroy()
    {
        
    }
}
