using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace DataArchitecture
{
    
    /// <summary>
    /// int is the key
    ///
    /// ownership of data is local to the system using it.
    /// owner system must dispose when the system is no longer in use.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public unsafe struct Data<T> where T : unmanaged
    {
        public int currentSize => nextIndex;
        int nextIndex;
        UnsafeList<T> data;
        
        public Data() => throw new System.NotImplementedException("Use Data(int capacity, Allocator allocator) constructor to initialize with a specific capacity and allocator.");

        public Data(int capacity, Allocator allocator)
        {
            nextIndex = 0;
            data = new UnsafeList<T>(capacity, allocator);
        }

        /// <summary>
        /// Allocates a new element and returns its stable index.
        /// Capacity is reserved memory space, can be uninitialized memory that points to random stuff
        /// Resize() adjusts Length, which is the number of initialized elements.
        /// </summary>
        /// <param name="_data">Data to store.</param>
        /// <returns>Allocated element index.</returns>
        public int Allocate(ref T _data)
        {
            if (nextIndex >= data.Capacity) data.SetCapacity(math.max(1, data.Capacity * 2));
            
            data.Resize(nextIndex + 1);
            data[nextIndex] = _data;
            return nextIndex++;
        }
        public void Allocate(T _data, out int allocationId) => allocationId = Allocate(ref _data);

        
        
        public ref T this[int id] => ref GetData(id);
        ref T GetData(int id)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if ((uint)id >= (uint)currentSize) throw new System.IndexOutOfRangeException($"Index {id} out of bounds. Data length is {data.Length}");
#endif
            return ref data.ElementAt(id);
        }

        public void Dispose()
        {
            if (data.IsCreated) data.Dispose();
        }
    }
    
}
