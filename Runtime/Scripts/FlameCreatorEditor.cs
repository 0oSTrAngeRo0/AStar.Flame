#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEngine;

namespace AStar.Flame
{
    public partial class FlameCreator
    {
        private void ExportFbx(GameObject go)
        {
            string path = AssetDatabase.GetAssetPath(m_AdditiveFlameData);
            string filename = $"{Path.GetFileNameWithoutExtension(path)}.fbx";
            string directory = Path.GetDirectoryName(path);
            path = Path.Combine(directory, filename);
            ModelExporter.ExportObject(path, go, new ExportModelOptions
            {
                ExportFormat = ExportFormat.Binary,
                ObjectPosition = ObjectPosition.LocalCentered,
                ExportUnrendered = false,
                PreserveImportSettings = false,
                KeepInstances = false,
                EmbedTextures = false,
                ModelAnimIncludeOption = Include.Model,
                LODExportType = LODExportType.All,
                AnimateSkinnedMesh = false,
                UseMayaCompatibleNames = true,
                AnimationSource = go.transform,
                AnimationDest = go.transform
            });

            SetupImporter(path);

            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(path));
        }

        private void SetupImporter(string path)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) return;
            
            // model.scene
            importer.globalScale = 1.0f;
            importer.useFileUnits = true;
            importer.bakeAxisConversion = false;
            importer.importBlendShapes = true;
            importer.importBlendShapeDeformPercent = false;
            importer.importVisibility = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.preserveHierarchy = false;
            importer.sortHierarchyByName = true;
            
            // model.meshes
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = false;
            importer.meshOptimizationFlags = 0;
            importer.addCollider = false;

            // model.geometry
            importer.keepQuads = false;
            importer.weldVertices = false;
            importer.indexFormat = ModelImporterIndexFormat.UInt32;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importBlendShapeNormals = ModelImporterNormals.Calculate;
            importer.normalCalculationMode = ModelImporterNormalCalculationMode.AreaAndAngleWeighted;
            importer.normalSmoothingSource = ModelImporterNormalSmoothingSource.PreferSmoothingGroups;
            importer.normalSmoothingAngle = 60.0f;
            importer.importTangents = ModelImporterTangents.Import;
            importer.swapUVChannels = false;
            importer.strictVertexDataChecks = false;

            // rig
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.skinWeights = ModelImporterSkinWeights.Custom;
            importer.minBoneWeight = 0.0f;
            importer.motionNodeName = "Root";
            importer.optimizeBones = false;
            importer.maxBonesPerVertex = 5;
            importer.optimizeGameObjects = false;

            // animation
            importer.importAnimation = false;
            importer.importConstraints = false;

            // material
            importer.materialImportMode = ModelImporterMaterialImportMode.None;

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }
    }
}

#endif