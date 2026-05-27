using System;
using System.Runtime.InteropServices;
using ProArchitecture.Logic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace ProArchitecture.Data
{

    public static class Batcher
    {
        public static void Process<T>(ref Data<T> data, Logics<T> logics) where T : unmanaged
        {
            for(int i = 0; i < data.currentSize; i++)
            {
                ref Data<T>.DataWrapper element = ref data.GetWrapper(i);
                if(!element.Active) continue;
                logics.TryRunAllSequentially(ref element.DataVolatile);
            }
        }
    }
    
    public struct ByteBool
    {
        byte value;
        public bool active => value != 0;
        public void Set(bool _value) => value = (byte)(_value ? 1 : 0);
        public static implicit operator bool(ByteBool b) => b.active;
        public static implicit operator ByteBool(bool b) => new ByteBool { value = (byte)(b ? 1 : 0) };
        public ByteBool(bool _value) => value = (byte)(_value ? 1 : 0);
    }

    
    /// <summary>
    /// Use when you need to mutate managed references in an unmanaged or unsafe context
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct BlittableManagedReference<T> where T : class
    {
        IntPtr Handle;
        readonly bool IsAllocated => Handle != IntPtr.Zero;
        public static BlittableManagedReference<T> Allocate(T target) => new() { Handle = GCHandle.ToIntPtr(GCHandle.Alloc(target)) };
        public readonly T Target
        {
            get { if (!IsAllocated) throw new InvalidOperationException("The managed reference has not been allocated or has already been Freed");
                return (T)((GCHandle)Handle).Target; }
        }
        public void Free()
        {
            if (Handle == IntPtr.Zero) return;
            ((GCHandle)Handle).Free();
            Handle = IntPtr.Zero;
        }
    }
    
    
    /// <summary>
    /// What is this?
    /// single component, low level, cache friendly, no gc, data store
    ///
    /// Why this over NativeList<T>?
    /// - NativeList<T> mutations use cpu cycles to copy data to and from the stack
    /// - this avoids that and modified the actual using ref T
    /// - uses Object Pooling Defragmentation Strategy to reuse indicies and avoid fragmentation and gc pressure
    /// 
    /// Usage:
    /// int is stable key
    /// ownership of data is local to the system using it.
    /// owner system must dispose when the system is no longer in use.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Data<T> where T : unmanaged
    {
        /// <summary>
        /// For:
        /// - "Removal" of unsued Indicies from batch processes
        /// - Object pooling
        /// </summary>
        public unsafe struct DataWrapper
        {
            public ByteBool Active;
            T data;
            public DataWrapper() => throw new System.NotImplementedException("This struct is only a wrapper for the Data struct, and should not be initialized directly.");
            public DataWrapper(T getData)
            {
                Active = new ByteBool();
                Active.Set(true);
                this.data = getData;
            }

            /// <summary>
            /// During Allocation, if SetCapacity is called, these refs will point to nothing
            /// Don't store these if you are unsure about potential capacity cahnges
            /// </summary>
            public ref T DataVolatile
            {
                get
                {
                    fixed (T* ptr = &data)
                        return ref *ptr;
                }
            }
        }
        
        
        public int currentSize => nextIndex;
        public ByteBool Active;
        int nextIndex;
        internal UnsafeList<DataWrapper> data;
        
        public Data() => throw new System.NotImplementedException("Use Data(int capacity, Allocator allocator) constructor to initialize with a specific capacity and allocator.");

        public Data(int capacity, Allocator allocator)
        {
            nextIndex = 0;
            data = new UnsafeList<DataWrapper>(capacity, allocator);
            Active = new ByteBool();
            Active.Set(true);
        }

        public Data(Data<T> tempAllocatedEvents)
        {
            nextIndex = tempAllocatedEvents.nextIndex;
            data = tempAllocatedEvents.data;
            Active = tempAllocatedEvents.Active;
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
            data[nextIndex] = new DataWrapper(_data);
            return nextIndex++;
        }
        public void Allocate(ref T _data, out int allocationId) => allocationId = Allocate(ref _data);
        
        
        /// <summary>
        /// Used for Object Pooling to reallocate unsued indicies
        /// </summary>
        /// <param name="id"></param>
        /// <param name="newData"></param>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public void ReAllocateInactive(int id, ref T newData)
        {
            if(id >= currentSize) throw new System.IndexOutOfRangeException($"Index {id} out of bounds. Data length is {currentSize}");
            if(data[id].Active) throw new System.IndexOutOfRangeException($"Index {id} Trying to reallocate an active element.");
            data[id] = new DataWrapper(newData);
        }
        
        public ref T this[int id] => ref Get(id);

        /// <summary>
        /// used by the batcher to get the data
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public ref DataWrapper GetWrapper(int id)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if ((uint)id >= (uint)currentSize) throw new System.IndexOutOfRangeException($"Index {id} out of bounds. Data length is {data.Length}");
#endif
            return ref data.ElementAt(id); 
        }

        /// <summary>
        /// Use directly to get the data.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ref T Get(int id)
        {
            ref DataWrapper element = ref GetWrapper(id);
            return ref element.DataVolatile;
        }
        
        
        public void Dispose()
        {
            if (data.IsCreated)
            {
                data.Dispose();
                Active.Set(false);
            }
        }
    }
    
    
    // Notes on v3 (11:31 pm, may 21)
    // refactored system with validation, added summary, added public ctor throw, removed length manipultation by ctor
    // allocate is dynamic and resizes when capacity changes
    
    // Development of batcher, initially thought this was good, realized that data.active is not a valid check, need to be isntanced at the UnsafeLIst<T>[] level
    // for(int i = 0; i < data.currentSize; i++)
    // {
    //     ref T element = ref data[i];
    //     if(!data.active) continue;
    //     logic.TryRun(ref element);
    // }
    // so i need to wrap it in a struct
    
    // development of the wrapper
    // We replace the 'ref get' with a pointer-based ref return
    // the getting of the internal data initially wasnt going to work because unity disallows ref returns of struct fields,
    // but this is circumvented by using a pointer and fixed statement to get a ref to the data field.
    // This allows us to have the wrapper struct contain the active flag and the data, while still allowing us to get a ref
    // to the data for direct mutation of the internal data
    //
    // initilaly it looked like
    //public ref T Data => return ref data;
    //
    // final:
    // public ref T Data
    // {
    // get
    // {
    //     fixed (T* ptr = &data)
    //         return ref *ptr;
    // }
    // }
    
}
