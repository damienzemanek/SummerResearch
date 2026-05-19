using UnityEngine;

namespace LogicArchitecture
{
    public unsafe struct LogicFunctionTable<T> where T : unmanaged
    {
        internal delegate*<T*, void> Run;
        internal delegate*<T*, bool> ShouldRun;
    }

    public readonly struct LogicHandle<T> where T : unmanaged
    {
        readonly LogicFunctionTable<T> table;
        
        internal LogicHandle(LogicFunctionTable<T> table) => this.table = table;

        public unsafe void Run(ref T data)
        {
            fixed (T* ptr = &data)
                table.Run(ptr); // ptr NOT out of scope
        }
        public unsafe bool ShouldRun(in T data)
        {
            fixed (T* ptr = &data)
                return table.ShouldRun(ptr);// ptr NOT out of scope
        }
    }
}