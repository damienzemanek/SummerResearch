using LogicArchitecture;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public unsafe class LogicBuffer<T> where T : unmanaged
{
    // static UnsafeList<LogicOperation<T>>* allOperations;
    //
    // public static void Init(int capacity) =>
    //     allOperations = UnsafeList<LogicOperation<T>>.Create(capacity, Allocator.Persistent);
    //
    // public static LogicOperation<T>* GetOperation(int index) => allOperations->Ptr + index;
}
