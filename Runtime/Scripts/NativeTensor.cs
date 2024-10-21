using System;
using Unity.Collections;

namespace AStar.Flame
{
    public struct NativeTensor<T> : IDisposable where T : unmanaged
    {
        public NativeArray<int> Shape;
        public NativeArray<T> Data;

        public void Dispose()
        {
            Shape.Dispose();
            Data.Dispose();
        }
    }
}