using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace AStar.Flame
{
    public class FlameCreator : MonoBehaviour
    {
        [SerializeField] private TextAsset m_FlameData;
        [SerializeField] private TextAsset m_AdditiveFlameData;
        [SerializeField] private Material m_Material;

        [ContextMenu(nameof(CreateFlameHead))]
        private void CreateFlameHead()
        {
            Assert.IsNotNull(m_FlameData);
            Assert.IsNotNull(m_AdditiveFlameData);
            var go = new GameObject(m_AdditiveFlameData.name);
            go.transform.SetParent(transform);
            var smr = FlameImportX.CreateFlameHead(go, m_FlameData.text, m_AdditiveFlameData.text);
            smr.material = m_Material == null
                ? GraphicsSettings.currentRenderPipeline.defaultMaterial
                : m_Material;
            smr.gameObject.AddComponent<FlameSmrAttachment>();
        }
    }
}