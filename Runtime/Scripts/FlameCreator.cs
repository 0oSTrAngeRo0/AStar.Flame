using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace AStar.Flame
{
    public partial class FlameCreator : MonoBehaviour
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

            var smr = FlameImportX.CreateFlameHead(GetImporterConfig(go));

            var material = m_Material;
            if (material == null) material = GraphicsSettings.currentRenderPipeline.defaultMaterial;
            smr.material = material;

            smr.gameObject.AddComponent<FlameSmrAttachment>();

            #if UNITY_EDITOR
            ExportFbx(go);
            go.Destroy();
            #endif
        }

        private FlameImportX.CreateConfig GetImporterConfig(GameObject attach) => new FlameImportX.CreateConfig
        {
            AttachGo = attach,
            FlameBaseJson = m_FlameData.text,
            FlameAdditiveJson = m_AdditiveFlameData.text,
        };
    }
}