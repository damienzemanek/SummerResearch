using System;
using Unity.Collections;


namespace ProArchitecture.Predicates
{

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