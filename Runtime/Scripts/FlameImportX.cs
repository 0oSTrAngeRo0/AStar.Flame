using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace AStar.Flame
{
    public static class FlameImportX
    {
        [BurstCompile]
        private struct CalculateJointsJob : IJobFor
        {
            public int JointsCount;
            public int VertexCount;

            [ReadOnly, NativeDisableParallelForRestriction]
            public NativeArray<float> Weights;

            [ReadOnly, NativeDisableParallelForRestriction]
            public NativeArray<float3> Vertices;

            [WriteOnly] public NativeArray<float3> Joints;

            public CalculateJointsJob(NativeTensor<float> jRegressor, NativeArray<float3> vertices,
                Allocator allocator = Allocator.Persistent)
            {
                Assert.AreEqual(jRegressor.Shape.Length, 2);
                Assert.AreEqual(jRegressor.Shape[1], vertices.Length);
                JointsCount = jRegressor.Shape[0];
                VertexCount = vertices.Length;
                Vertices = vertices;
                Weights = jRegressor.Data;
                Joints = new NativeArray<float3>(JointsCount, allocator);
            }


            public void Execute(int index)
            {
                int start = index * VertexCount;
                float3 joint = float3.zero;
                for (int i = 0; i < VertexCount; i++)
                    joint += Weights[start + i] * Vertices[i];
                Joints[index] = joint;
            }

            public JobHandle Dispatch(int batch = 1, JobHandle dependency = default) =>
                this.ScheduleParallel(Joints.Length, batch, dependency);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddBlendShapes(Mesh mesh, NativeTensor<float3> tensor, Func<int, string> genName)
        {
            Assert.AreEqual(tensor.Shape.Length, 2);
            int bsCount = tensor.Shape[0];
            AddBlendShapes(mesh, tensor, 0, bsCount, genName);
        }

        private static void AddBlendShapes(
            Mesh mesh, NativeTensor<float3> tensor,
            int startShape, int shapeCount,
            Func<int, string> genName)
        {
            Assert.IsNotNull(genName);
            Assert.AreEqual(tensor.Shape.Length, 2);

            int bsCount = tensor.Shape[0];
            int vCount = tensor.Shape[1];

            Assert.AreEqual(mesh.vertexCount, vCount);
            Assert.IsTrue(startShape >= 0 && startShape < bsCount);
            Assert.IsTrue(startShape + shapeCount >= 0 && startShape + shapeCount <= bsCount);

            for (int bsIt = startShape, end = startShape + shapeCount; bsIt < end; bsIt++)
            {
                string name = genName.Invoke(bsIt);
                Vector3[] vertices = tensor.Data.Slice(bsIt * vCount, vCount).SliceConvert<Vector3>().ToArray();
                mesh.AddBlendShapeFrame(name, 1.0f, vertices, null, null);
            }
        }

        private static void SetBoneWeights(int jointCount, NativeTensor<float> weights, Mesh mesh)
        {
            Assert.AreEqual(weights.Shape.Length, 2);
            Assert.AreEqual(weights.Shape[1], jointCount);
            Assert.AreEqual(weights.Shape[0], mesh.vertexCount);

            int vCount = mesh.vertexCount;

            var boneCounts = new NativeArray<byte>(vCount, Allocator.Temp);
            for (int i = 0; i < vCount; i++)
                boneCounts[i] = (byte)jointCount;

            var weightsData = weights.Data;
            var weightCache = new BoneWeight1[jointCount];
            var boneWeights = new NativeArray<BoneWeight1>(jointCount * vCount, Allocator.Temp);
            for (int i = 0, end = mesh.vertexCount; i < end; i++)
            {
                int start = i * jointCount;
                for (int j = 0; j < jointCount; j++)
                {
                    weightCache[j].weight = weightsData[start + j];
                    weightCache[j].boneIndex = j;
                }

                Array.Sort(weightCache, (x, y) => y.weight.CompareTo(x.weight));
                for (int j = 0; j < jointCount; j++)
                    boneWeights[start + j] = weightCache[j];
            }

            mesh.SetBoneWeights(boneCounts, boneWeights);

            boneWeights.Dispose();
            boneCounts.Dispose();
        }

        private static void SetBoneWeights2(int jointCount, NativeTensor<float> weights, Mesh mesh)
        {
            Assert.AreEqual(weights.Shape.Length, 2);
            Assert.AreEqual(weights.Shape[1], jointCount);
            Assert.AreEqual(weights.Shape[0], mesh.vertexCount);

            int vCount = mesh.vertexCount;

            var weightsData = weights.Data;
            var weightCache = new BoneWeight1[jointCount];
            var boneWeights = new BoneWeight[vCount];
            for (int i = 0, end = mesh.vertexCount; i < end; i++)
            {
                int start = i * jointCount;
                for (int j = 0; j < jointCount; j++)
                {
                    weightCache[j].weight = weightsData[start + j];
                    weightCache[j].boneIndex = j;
                }

                Array.Sort(weightCache, (x, y) => y.weight.CompareTo(x.weight));

                boneWeights[i] = new BoneWeight
                {
                    weight0 = weightCache[0].weight,
                    weight1 = weightCache[1].weight,
                    weight2 = weightCache[2].weight,
                    weight3 = weightCache[3].weight,
                    boneIndex0 = weightCache[0].boneIndex,
                    boneIndex1 = weightCache[1].boneIndex,
                    boneIndex2 = weightCache[2].boneIndex,
                    boneIndex3 = weightCache[3].boneIndex
                };
            }

            mesh.boneWeights = boneWeights;
        }

        private static void ApplyAdditiveToSmr(FlameAdditiveData additive, SkinnedMeshRenderer smr)
        {
            Assert.AreEqual(smr.sharedMesh.blendShapeCount, additive.ShapeWeights.Length);
            Assert.AreEqual(additive.StaticOffset.Length, smr.sharedMesh.vertexCount);

            for (int i = 0; i < additive.ShapeWeights.Length; i++)
                smr.SetBlendShapeWeight(i, additive.ShapeWeights[i]);

            var mesh = smr.sharedMesh;
            var staticOffset = additive.StaticOffset.Reinterpret<Vector3>().ToArray();
            string bsName = nameof(additive.StaticOffset);
            mesh.AddBlendShapeFrame(bsName, 1.0f, staticOffset, null, null);
            int bsIndex = mesh.GetBlendShapeIndex(bsName);
            smr.SetBlendShapeWeight(bsIndex, 1.0f);
        }

        private static void CreateJoints(NativeArray<int> tree, NativeArray<float3> joints, SkinnedMeshRenderer smr)
        {
            Assert.AreEqual(tree.Length, joints.Length);
            Assert.IsNotNull(smr.transform.parent);
            Assert.AreEqual(tree.Count(parent => parent >= joints.Length || parent < 0), 1, "Invalid root count");
            Transform[] transforms = new Transform[joints.Length];
            string[] names = { "Root", "Neck", "Jaw", "RightEye", "LeftEye" };
            for (int i = 0, end = transforms.Length; i < end; i++)
                transforms[i] = new GameObject(names[i]).transform;
            for (int i = 0, end = tree.Length; i < end; i++)
            {
                int parent = tree[i];
                if (parent >= joints.Length || parent < 0)
                {
                    transforms[i].SetParent(smr.transform.parent);
                    smr.rootBone = transforms[i];
                    transforms[i].position = Vector3.zero;
                }
                else
                {
                    transforms[i].SetParent(transforms[parent]);
                    transforms[i].position = joints[i];
                }
            }

            smr.bones = transforms;

            Matrix4x4[] bindPoses = new Matrix4x4[joints.Length];
            for (int i = 0, end = bindPoses.Length; i < end; i++)
                bindPoses[i] = transforms[i].worldToLocalMatrix * smr.rootBone.localToWorldMatrix;
            smr.sharedMesh.bindposes = bindPoses;
        }

        private static void BindBones(SkinnedMeshRenderer smr, FlameData flame)
        {
            var mesh = smr.sharedMesh;

            var vertices = new NativeArray<Vector3>(mesh.vertices, Allocator.Persistent).Reinterpret<float3>();
            var job = new CalculateJointsJob(flame.JRegressor, vertices);
            job.Dispatch().Complete();
            var joints = job.Joints;
            vertices.Dispose();

            SetBoneWeights(joints.Length, flame.Weights, mesh);
            CreateJoints(flame.KintreeTable, joints, smr);

            joints.Dispose();
        }

        private static Mesh CreateBaseMesh(FlameData flame, FlameAdditiveData additive)
        {
            int shapeCount = additive.ShapeWeights.Length;
            Mesh mesh = new Mesh();
            mesh.SetVertices(flame.VTemplate);
            mesh.SetIndices(flame.Faces.Data, MeshTopology.Triangles, 0);
            AddBlendShapes(mesh, flame.ShapeDirs, 0, shapeCount, iter => $"Shape {iter}");
            mesh.RecalculateNormals();
            return mesh;
        }

        private static void ApplyFlame(SkinnedMeshRenderer smr, FlameData flame, FlameAdditiveData additive)
        {
            Mesh baseMesh = CreateBaseMesh(flame, additive);
            smr.sharedMesh = baseMesh;
            ApplyAdditiveToSmr(additive, smr);

            Mesh baked = new Mesh();
            smr.BakeMesh(baked, false);
            baked.RecalculateNormals();
            smr.sharedMesh = baked;

            BindBones(smr, flame);

            AddBlendShapes(baked, flame.PoseDirs, iter => $"Pose {iter}"); // 添加pose类型的blendshape 
            int shapeBsCount = additive.ShapeWeights.Length; // shape类型的blendshape数量
            int exprBsCount = flame.ShapeDirs.Shape[0] - shapeBsCount; // expression类型的blendshape数量
            AddBlendShapes(baked, flame.ShapeDirs, shapeBsCount, exprBsCount,
                iter => $"Expression {iter - shapeBsCount}"); // 添加expression类型的blendshape 

            for (int i = 0; i < baked.blendShapeCount; i++)
                smr.SetBlendShapeWeight(i, 0.0f);
        }

        public static SkinnedMeshRenderer CreateFlameHead(GameObject attach, string flameJson, string additiveJson)
        {
            using var flame = new FlameData(JsonUtility.FromJson<JsonData>(flameJson));
            using var additive = new FlameAdditiveData(JsonUtility.FromJson<JsonData>(additiveJson));
            var go = new GameObject("Mesh");
            go.transform.SetParent(attach.transform);
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            ApplyFlame(smr, flame, additive);
            return smr;
        }
    }
}