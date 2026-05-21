using System;
using UnityEngine;

namespace LogicArchitecture
{
    


    /// <summary>
    /// Handle that stores static logic operations on instance data, as an instance
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly unsafe struct LogicOperation<T> where T : unmanaged
    {
        delegate*<T*, void> run { get; }
        delegate*<T*, bool> shouldRun { get; }        
        
        public LogicOperation() => throw new Exception("Do not use the default ctor, call MySystemLogic.Handle");
        internal LogicOperation(delegate*<T*, void> _run, delegate*<T*, bool> _shouldRun) 
        {
            run = _run;
            shouldRun = _shouldRun;
        }
        
        internal LogicOperation<T>* Ptr 
        {
            get { fixed (LogicOperation<T>* ptr = &this) return ptr; }
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
    
    public readonly unsafe struct LogicHandle<T> where T : unmanaged
    {
        readonly LogicOperation<T>* operations;
        readonly int count;

        public LogicHandle(LogicOperation<T>* _operations, int count)
        {
            operations = _operations;
            this.count = count;
        }

        public void Run(ref T data)
        {
            for (int i = 0; i < count; i++)
                operations[i].Run(ref data);
        }



        public static implicit operator LogicHandle<T>(LogicOperation<T>* stableOpPtr)
        {
            return new LogicHandle<T>(stableOpPtr, 1);
        }
    }
}