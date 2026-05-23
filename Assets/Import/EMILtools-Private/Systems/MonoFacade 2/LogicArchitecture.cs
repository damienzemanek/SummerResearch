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
        readonly delegate*<T*, void> run;
        readonly delegate*<T*, bool> shouldRun;        
        
        public LogicOperation(delegate*<T*, void> _run, delegate*<T*, bool> _shouldRun) 
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
    
    public readonly unsafe struct Logics<T> where T : unmanaged
    {
        public readonly LogicOperation<T>* operations;
        public readonly int count;

        public int Count => count;

        public Logics(LogicOperation<T>* _operations, int count)
        {
            operations = _operations;
            this.count = count;
        }

        public void TryRun(ref T data)
        {
            for (int i = 0; i < count; i++)
                if (operations[i].ShouldRun(data))
                    operations[i].Run(ref data);
        }
        

        public static implicit operator Logics<T>(LogicOperation<T>* stableOpPtr)
        {
            return new Logics<T>(stableOpPtr, 1);
        }
    }
}