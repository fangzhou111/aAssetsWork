using System;
using UnityEditor;
using UnityEngine;

namespace SuperMobs.AssetManager.Editor
{
    public class ImporterFbx : AssetPostprocessor
    {
        void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
        }

        void OnPreprocessModel()
        {
            ModelImporter modelImporter = assetImporter as ModelImporter;
            //modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
            //modelImporter.meshCompression = ModelImporterMeshCompression.Medium;
            modelImporter.importTangents = ModelImporterTangents.CalculateMikk;
            modelImporter.animationCompression = ModelImporterAnimationCompression.Optimal;
            modelImporter.optimizeMeshPolygons = true;
        }

        Material OnAssignMaterialModel(Material material, Renderer renderer)
        {
            var materialPath = "Assets/Resources/default-fbx.mat";

            if (AssetDatabase.LoadAssetAtPath<Material>(materialPath))
                return AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            material.shader = Shader.Find("Standard");
            AssetDatabase.CreateAsset(material, materialPath);
            AssetDatabase.ImportAsset(materialPath);

            return AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        }
    }
}