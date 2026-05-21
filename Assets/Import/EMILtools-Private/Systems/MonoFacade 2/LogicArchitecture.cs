using System;
using UnityEngine;

namespace LogicArchitecture
{


    /// <summary>
    /// 
    /// 
    /// main API hook for internal logic
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly unsafe struct LogicHandle<T> where T : unmanaged
    {
        delegate*<T*, void> run { get; }
        delegate*<T*, bool> shouldRun { get; }        
        
        public LogicHandle() => throw new Exception("Do not use the default ctor, call MySystemLogic.Handle");
        internal LogicHandle(delegate*<T*, void> _run, delegate*<T*, bool> _shouldRun) 
        {
            run = _run;
            shouldRun = _shouldRun;
        }

        /// <summary>
        /// converts the ref T to a pointer, and calls the function
        /// </summary>
        /// <param name="data"></param>
        public void Run(ref T data)
        {
            fixed (T* ptr = &data) run(ptr);
        }
        
        
        /// <summary>
        /// converst the in T to a pointer, and calls the function
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool ShouldRun(in T data)
        {
            fixed (T* ptr = &data) return shouldRun(ptr);
        }
    }
}