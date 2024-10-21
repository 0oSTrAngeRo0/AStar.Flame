using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace AStar.Flame
{
    [Serializable]
    public struct JsonDataFrame
    {
        public string name;
        public string type;
        public int[] shape;
        public string data;
    }

    [Serializable]
    public struct JsonData
    {
        public JsonDataFrame[] data;
    }


    public struct FlameData : IDisposable
    {
        public NativeTensor<uint> Faces;
        public NativeTensor<float> JRegressor;
        public NativeArray<int> KintreeTable;
        public NativeTensor<float> J;
        public NativeTensor<float> Weights;
        public NativeTensor<float3> PoseDirs;
        public NativeArray<float3> VTemplate;
        public NativeTensor<float3> ShapeDirs;

        public FlameData(JsonData data)
        {
            Faces = data.GetTensor<uint>("f");
            JRegressor = data.GetTensor<float>("J_regressor");
            KintreeTable = data.GetArray<int>("kintree_table");
            J = data.GetTensor<float>("J");
            Weights = data.GetTensor<float>("weights");
            PoseDirs = data.GetBlendShapes("posedirs");
            VTemplate = data.GetArray<float3>("v_template");
            ShapeDirs = data.GetBlendShapes("shapedirs");
        }

        public void Dispose()
        {
            Faces.Dispose();
            JRegressor.Dispose();
            KintreeTable.Dispose();
            J.Dispose();
            Weights.Dispose();
            PoseDirs.Dispose();
            VTemplate.Dispose();
            ShapeDirs.Dispose();
        }
    }

    public struct FlameAdditiveData : IDisposable
    {
        public NativeArray<float> ShapeWeights;
        public NativeArray<float3> StaticOffset;

        public FlameAdditiveData(JsonData data)
        {
            ShapeWeights = data.GetArray<float>("shape");
            StaticOffset = data.GetArray<float3>("static_offset");
        }
        
        public void Dispose()
        {
            ShapeWeights.Dispose();
            StaticOffset.Dispose();
        }
    }

    public static class JsonDataX
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JsonDataFrame GetFrame(this JsonData data, string key) =>
            data.data.First(item => item.name == key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeTensor<T> GetTensor<T>(this JsonData data, string key) where T : unmanaged =>
            data.GetFrame(key).GetTensor<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeArray<T> GetArray<T>(this JsonData data, string key) where T : unmanaged =>
            data.GetFrame(key).GetData<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeTensor<float3> GetBlendShapes(this JsonData data, string key)
        {
            var frame = data.GetFrame(key);
            Assert.AreEqual(frame.shape.Length, 3);
            Assert.AreEqual(frame.shape[2], 3);
            return new NativeTensor<float3>()
            {
                Shape = new NativeArray<int>(new int[] { frame.shape[0], frame.shape[1] }, Allocator.Persistent),
                Data = frame.GetData<float3>()
            };
        }
    }

    public static class JsonDataFrameX
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeArray<byte> GetData(this JsonDataFrame frame,
            Allocator allocator = Allocator.Persistent) => frame.GetData<byte>(allocator);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeArray<T> GetData<T>(this JsonDataFrame frame,
            Allocator allocator = Allocator.Persistent)
            where T : unmanaged
            => new NativeArray<byte>(Convert.FromBase64String(frame.data), allocator).Reinterpret<T>(1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeTensor<T> GetTensor<T>(this JsonDataFrame frame,
            Allocator allocator = Allocator.Persistent)
            where T : unmanaged => new NativeTensor<T>
        {
            Shape = new NativeArray<int>(frame.shape, allocator),
            Data = frame.GetData<T>(allocator)
        };
    }
}