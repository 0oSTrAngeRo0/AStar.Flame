using System.Runtime.CompilerServices;

namespace AStar.Flame
{
    public static class UnityObjectX
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Destroy(this UnityEngine.Object obj)
        {
            #if UNITY_EDITOR
            UnityEngine.Object.DestroyImmediate(obj);
            #else
            UnityEngine.Object.Destroy(obj);
            #endif
        }
    }
}