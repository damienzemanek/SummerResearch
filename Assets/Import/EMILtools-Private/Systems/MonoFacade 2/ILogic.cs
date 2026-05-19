using UnityEngine;

namespace LogicArchitecture
{
    public unsafe struct LogicFunctionTable<T> where T : unmanaged
    {
        internal delegate*<T*, bool> ShouldRun;
        internal delegate*<T*, void> Run;
    }

    public readonly struct LogicHandle<T> where T : unmanaged
    {
        readonly LogicFunctionTable<T> table;
        
        internal LogicHandle(LogicFunctionTable<T> table) => this.table = table;

        public unsafe void Run(ref T data)
        {
            fixed (T* ptr = &data)
                table.Run(ptr);
        }

        public unsafe bool ShouldRun(in T data)
        {
            fixed (T* ptr = &data)
                return table.ShouldRun(ptr);
        }
    }
}