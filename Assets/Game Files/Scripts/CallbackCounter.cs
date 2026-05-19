using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CallbackCounter : MonoBehaviour
{
    int count = -1;

    [SerializeField]
    List<UnityEvent> events;

    public void Increment()
    {
        count++;

        if (count >= events.Count)
            return;

        events[count]?.Invoke();
    }
}