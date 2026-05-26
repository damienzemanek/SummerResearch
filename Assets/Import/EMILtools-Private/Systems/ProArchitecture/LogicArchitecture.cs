using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ProArchitecture.Logic
{
    
    public static unsafe class NativeListExtensions
    {
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(this ref NativeList<T> list) where T : unmanaged => new(list.GetUnsafeReadOnlyPtr(), list.Length);
    }


    
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
        
        // Single Ctor
        public Logics(ref LogicOperation<T> op)
        {
            operations = (LogicOperation<T>*)UnsafeUtility.AddressOf(ref op);
            count = 1;
        }
        
        // Multi Operation(s)
        public Logics(in ReadOnlySpan<LogicOperation<T>> ops)
        {
            if(ops.Length == 0) throw new ArgumentException("Operations array cannot be empty");
            operations = (LogicOperation<T>*)UnsafeUtility.AddressOf(ref MemoryMarshal.GetReference(ops));
            count = ops.Length;
        }
        
        
        
        /// <summary>
        /// Use if you want to call all operations sequentially
        /// </summary>
        /// <param name="data"></param>
        public void TryRunAllSequentially(ref T data)
        {
            for (int i = 0; i < count; i++)
                if (operations[i].ShouldRun(in data))
                    operations[i].Run(ref data);
        }
        
        /// <summary>
        /// Use if you want to call operations individually
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void TryRun(ref T data, int index)
        {
            if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index), "Index out of range");
            if (operations[index].ShouldRun(in data))
                operations[index].Run(ref data);
        }
    }
    
    public static unsafe class TickLogic<TData> where TData : unmanaged
    {
        public struct TickLogicData<TData> where TData : unmanaged
        {
            public float deltaTime;
            readonly Logics<TData>* coreLogics;
            public ref Logics<TData> CoreLogics => ref *coreLogics;
            readonly TData* coreData;
            public ref TData CoreData => ref *coreData;
            public TickLogicData(float _deltaTime, ref Logics<TData> _coreLogics, ref TData _coreData)
            {
                deltaTime = _deltaTime;
                coreLogics = (Logics<TData>*)Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AddressOf(ref _coreLogics);
                coreData = (TData*)Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AddressOf(ref _coreData);
            }
        }
    
        // concrete impementations
        // Deref pointer satisfies ref T param on TryRun
        static void Run(TickLogicData<TData>* data) => data->CoreLogics.TryRunAllSequentially(ref data->CoreData);
        static bool ShouldRun(TickLogicData<TData>* data) => true;
    
        // Tick Logic (this specfici implementation) only has 1 operation
        // When used in state logic, it will be added as an operation ITSELF to another Logics
        static readonly LogicOperation<TickLogicData<TData>> TickOperation = new(&Run, &ShouldRun); 
        public static readonly Logics<TickLogicData<TData>> TickLogics = new (ref TickOperation);
    }
}

// Note another way to convert a ref struct into a pointer is by using
// (MyStruct*)UnsafeUtility.AddressOf(ref myStruct)
// To param in multiuple of the same ref struct use ReadonlySpan, and Memorymarshal its reference
// public readonly LogicOperation<T>* operations;    
// operations = (LogicOperation<T>*)UnsafeUtility.AddressOf(ref MemoryMarshal.GetReference(ops));      