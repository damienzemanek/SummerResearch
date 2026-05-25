using System.Collections;
using UnityEngine;

public readonly struct CachedCoroutine
{
    readonly Coroutine routine;
    readonly WaitForSeconds wait;
    public CachedCoroutine(Coroutine routine, WaitForSeconds wait)
    {
        this.routine = routine;
        this.wait = wait;
    }
    public Coroutine Routine => routine;
}
