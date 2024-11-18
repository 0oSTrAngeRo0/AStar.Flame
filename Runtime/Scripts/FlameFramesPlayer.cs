using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace AStar.Flame
{
    [Serializable]
    public struct FlameFrame
    {
        public float3 RootPosition;
        public quaternion RootRotation;
        public quaternion NeckRotation;
        public quaternion JawRotation;
        public quaternion LeftEyeRotation;
        public quaternion RightEyeRotation;
        public NativeSlice<float> Expressions;
    }

    public struct FlameFrames : IDisposable
    {
        public struct EyeRotation
        {
            public float3 Left;
            public float3 Right;
        }

        public uint FrameCount;
        public uint ExpressionCount;
        public NativeArray<float3> RootPositions;
        public NativeArray<float3> RootRotations;
        public NativeArray<float3> NeckRotations;
        public NativeArray<float3> JawRotations;
        public NativeArray<EyeRotation> EyeRotations;
        public NativeArray<float> Expressions;

        public FlameFrames(JsonData json)
        {
            RootPositions = json.GetArray<float3>(Constants.Frames.ROOT_POSITION);
            RootRotations = json.GetArray<float3>(Constants.Frames.ROOT_ROTATION);
            NeckRotations = json.GetArray<float3>(Constants.Frames.NECK_ROTATION);
            JawRotations = json.GetArray<float3>(Constants.Frames.JAW_ROTATION);
            EyeRotations = json.GetArray<EyeRotation>(Constants.Frames.EYE_ROTATION);
            Expressions = json.GetArray<float>(Constants.Frames.EXPRESSIONS);
            FrameCount = 0;
            ExpressionCount = 0;
            if (!IsValidate(out var error))
            {
                Dispose();
                throw new Exception($"[{nameof(FlameFrames)}]Invalid JsonData: {error}");
            }

            FrameCount = (uint)RootPositions.Length;
            ExpressionCount = (uint)(Expressions.Length / FrameCount);
        }

        public bool IsValidate(out string error)
        {
            error = null;
            if (!RootPositions.IsCreated || !RootRotations.IsCreated || !NeckRotations.IsCreated ||
                !JawRotations.IsCreated || !EyeRotations.IsCreated || !Expressions.IsCreated)
            {
                error = "Array has not created";
                return false;
            }

            int frameCount = RootPositions.Length;
            if (frameCount != RootRotations.Length || frameCount != NeckRotations.Length ||
                frameCount != JawRotations.Length || frameCount != EyeRotations.Length ||
                Expressions.Length % frameCount != 0)
            {
                error = "Mismatch data frames";
                return false;
            }

            if (Expressions.Length / frameCount != Constants.MeshBs.EXPRESSION_COUNT)
            {
                error = "Mismatch expression count";
                return false;
            }

            return true;
        }

        public FlameFrame Get(int index) => new FlameFrame
        {
            RootPosition = RootPositions[index],
            RootRotation = quaternion.Euler(RootRotations[index]),
            NeckRotation = quaternion.Euler(NeckRotations[index]),
            JawRotation = quaternion.Euler(JawRotations[index]),
            LeftEyeRotation = quaternion.Euler(EyeRotations[index].Left),
            RightEyeRotation = quaternion.Euler(EyeRotations[index].Right),
            Expressions = Expressions.Slice((int)(index * ExpressionCount), (int)ExpressionCount)
        };

        public bool TryGetFrame(int index, out FlameFrame frame)
        {
            frame = default;
            if (index < 0 || index >= FrameCount) return false;
            frame = Get(index);
            return true;
        }

        public void Dispose()
        {
            RootPositions.Dispose();
            RootRotations.Dispose();
            NeckRotations.Dispose();
            JawRotations.Dispose();
            EyeRotations.Dispose();
            Expressions.Dispose();
        }
    }

    [ExecuteInEditMode, RequireComponent(typeof(SkinnedMeshRenderer))]
    public partial class FlameFramesPlayer : MonoBehaviour
    {
        [SerializeField] private TextAsset m_Asset;
        private FlameFrames m_FlameFrames;
        private SkinnedMeshRenderer m_Smr;
        [SerializeField] private int m_CurrentFrameIndex;

        public int CurrentFrameIndex
        {
            get => m_CurrentFrameIndex;
            set
            {
                m_CurrentFrameIndex = value;
                OnValidate();
            }
        }

        private void OnEnable()
        {
            m_Smr = GetComponent<SkinnedMeshRenderer>();
            m_FlameFrames = new FlameFrames(JsonUtility.FromJson<JsonData>(m_Asset.text));
        }

        private void OnDisable()
        {
            m_FlameFrames.Dispose();
            ResetFrame();
        }

        private void OnValidate()
        {
            if (!enabled) return;
            m_CurrentFrameIndex = Mathf.Clamp(m_CurrentFrameIndex, 0, (int)m_FlameFrames.FrameCount);
            ApplyFrame();
        }

        private void ApplyFrame()
        {
            if (m_Smr == null) return;
            if (!m_FlameFrames.IsValidate(out var _)) return;
            if (!m_FlameFrames.TryGetFrame(m_CurrentFrameIndex, out FlameFrame frame)) return;
            for (int i = 0; i < Constants.MeshBs.EXPRESSION_COUNT; i++)
            {
                float value = frame.Expressions[i];
                int index = i + Constants.MeshBs.EXPRESSION_START_INDEX;
                m_Smr.SetBlendShapeWeight(index, value);
            }

            Transform[] bones = m_Smr.bones;
            bones[Constants.Bone.Index.ROOT].localPosition = frame.RootPosition;
            bones[Constants.Bone.Index.ROOT].localRotation = frame.RootRotation;
            bones[Constants.Bone.Index.NECK].localRotation = frame.NeckRotation;
            bones[Constants.Bone.Index.JAW].localRotation = frame.JawRotation;
            bones[Constants.Bone.Index.LEFT_EYE].localRotation = frame.LeftEyeRotation;
            bones[Constants.Bone.Index.RIGHT_EYE].localRotation = frame.RightEyeRotation;
        }

        private void ResetFrame()
        {
            if (m_Smr == null || m_Smr.sharedMesh == null) return;
            for (int i = 0, end = m_Smr.sharedMesh.blendShapeCount; i < end; i++)
                m_Smr.SetBlendShapeWeight(i, 0);
            Transform[] bones = m_Smr.bones;
            bones[Constants.Bone.Index.ROOT].localPosition = Vector3.zero;
            bones[Constants.Bone.Index.ROOT].localRotation = Quaternion.identity;
            bones[Constants.Bone.Index.NECK].localRotation = Quaternion.identity;
            bones[Constants.Bone.Index.JAW].localRotation = Quaternion.identity;
            bones[Constants.Bone.Index.LEFT_EYE].localRotation = Quaternion.identity;
            bones[Constants.Bone.Index.RIGHT_EYE].localRotation = Quaternion.identity;
        }
    }
}