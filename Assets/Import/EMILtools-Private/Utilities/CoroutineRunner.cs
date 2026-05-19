using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CoroutineUtility
{

    public class CoroutineRunner : MonoBehaviour { }

    public static void Run(this CoroutineRunner cr, Action hook, float delay)
    {
        if (cr == null) return;
        if (hook == null) return;

        cr.StartCoroutine(C_Run(hook, delay));
    }

    static IEnumerator C_Run(Action hook, float delay)
    {
        if(delay > 0) yield return new WaitForSecondsRealtime(delay);

        hook?.Invoke();
    }

}



