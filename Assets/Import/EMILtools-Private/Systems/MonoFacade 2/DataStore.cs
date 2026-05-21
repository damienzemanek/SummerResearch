using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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
        int nextIndex;
        UnsafeList<T> data;

        public Data(int capacity, Allocator allocator)
        {
            nextIndex = 0;
            data = new UnsafeList<T>(capacity, allocator);
            data.Length = capacity;
        }

        public int Allocate(ref T _data)
        {
            if (nextIndex >= data.Length) 
                data.Resize(data.Length * 2); 

            data[nextIndex] = _data;
            return nextIndex++;
        }
        
        public void Allocate(T _data, out int allocationId)
        {
            data[nextIndex] = _data;
            allocationId = nextIndex++;
        }
        
        public ref T this[int id] => ref data.ElementAt(id);
        public ref T GetData(int id) => ref data.ElementAt(id);

        public void Dispose()
        {
            if (data.IsCreated) data.Dispose();
        }
    }
    
}
