using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using DataArchitecture;
using LogicArchitecture;
using ProSMLogic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace StateArchitecture
{
    

    public struct Transition
    {
        public short to;
        public float duration;
        public Predicate condition;
        // mabye in the future make this a logic that does not have to pass in the predicate, but creates it here
        public Transition(short _to, ref Predicate _condition, float _duration = 0f) 
        {
            to = _to;
            condition = _condition;
            duration = _duration;
        }
    }



    /// <summary>
    /// Delegate* are blittable
    /// </summary>
    public unsafe struct Predicate
    {
        //      Subscribe using this delegate signature:
        //      bool IsSomething(ref T data);

        // 1st: void* is the delegate
        // 2nd: bool is the result
        internal delegate*<void*, bool> evaluate;
        public bool IsCreated => evaluate != null;
        public Predicate(delegate*<void*, bool> _evaluate) => evaluate = _evaluate;
    }

    public static unsafe class PredicateExtensions
    {
        public static bool Evaluate<T>(this ref Predicate predicate, ref T data) where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (predicate.evaluate == null) throw new NullReferenceException("Predicate not initialized");
#endif
            fixed(void* ptr = &data) return predicate.evaluate(ptr);
        }
    }
    
    




}