using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace AStar.Flame
{
    [ExecuteInEditMode, RequireComponent(typeof(SkinnedMeshRenderer))]
    public class FlameSmrAttachment : MonoBehaviour
    {
        [SerializeField] private bool m_EnablePoseCorrective;
        private SkinnedMeshRenderer m_Smr;

        private float[] FlattenMatrix(float3x3 matrix) => new float[]
        {
            matrix.c0.x, matrix.c0.y, matrix.c0.z,
            matrix.c1.x, matrix.c1.y, matrix.c1.z,
            matrix.c2.x, matrix.c2.y, matrix.c2.z
        };

        private float3x3 GetBoneMat(Transform trans) =>
            math.transpose(math.float3x3(trans.localRotation) - float3x3.identity);

        private void Update()
        {
            if (m_Smr == null)
            {
                m_Smr = GetComponentInChildren<SkinnedMeshRenderer>();
                if (m_Smr == null) return;
            }
            if (!m_EnablePoseCorrective)
            {
                for (int i = 0, end = 36; i < end; i++)
                    m_Smr.SetBlendShapeWeight(i, 0.0f);
            }
            else
            {
                var mats = m_Smr.bones.Skip(1).Select(GetBoneMat).ToArray();
                for (int i = 0; i < mats.Length; i++)
                {
                    var mat = FlattenMatrix(mats[i]);
                    for (int j = 0; j < mat.Length; j++)
                        m_Smr.SetBlendShapeWeight(i * mat.Length + j, mat[j]);
                }
            }
        }
    }
}