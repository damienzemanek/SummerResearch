using System;
using UnityEngine;
using UnityEngine.Events;

public class Encounterer : MonoBehaviour
{
    public UnityEvent Encounter;
    public bool encountered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (encountered) return;
            encountered = true;
            Encounter?.Invoke();
        }
    }
}
