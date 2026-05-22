using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using DataArchitecture;
using LogicArchitecture;
using Unity.Collections;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace StateArchitecture
{



    public struct StateData
    {
        const int TRANSITIONS_SIZE = 50;
        
        public int state;
        public Data<Transition> transitions;
        public StateData(int _state)
        {
            state = _state;
            transitions = new Data<Transition>(TRANSITIONS_SIZE, Allocator.Persistent);
        }
    }

    public struct Transition
    {
        public int to;
        public Predicate condition;
        // mabye in the future make this a logic that does not have to pass in the predicate, but creates it here
        public Transition(int _to, ref Predicate _condition)
        {
            to = _to;
            condition = _condition;
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