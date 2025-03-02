using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Uzi.Modeling.Editor
{
    public class ModelFileCodeGenerationOnAssetImport : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets, 
            string[] deletedAssets, 
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            HashSet<string> modelSetsToGenerate = null;
            CheckAssetPathsForModelFiles(importedAssets, ref modelSetsToGenerate);
            CheckAssetPathsForModelFiles(deletedAssets, ref modelSetsToGenerate);
            CheckAssetPathsForModelFiles(movedAssets, ref modelSetsToGenerate);
            CheckAssetPathsForModelFiles(movedFromAssetPaths, ref modelSetsToGenerate);

            if (modelSetsToGenerate != null)
            {
                foreach (var modelSet in modelSetsToGenerate)
                {
                    if (!File.Exists(modelSet))
                    {
                        continue;
                    }
                    var modelSetConfiguration =
                        JsonUtility.FromJson<ModelSetConfiguration>(
                                File.ReadAllText(modelSet));
                    var directory = new FileInfo(modelSet).Directory;
                    ModelCodeGeneration.Generate(GetAssetsRelativePath(directory.FullName), modelSetConfiguration);
                }
            }
        }

        static void CheckAssetPathsForModelFiles(string[] files, ref HashSet<string> modelSetsToGenerate)
        {
            foreach (var asset in files)
            {
                CheckModelFile(asset, ref modelSetsToGenerate);
            }
        }
        
        static void CheckModelFile(string assetPath, ref HashSet<string> modelSetsToGenerate)
        {
            if (Path.GetExtension(assetPath) == ".viewmodel")
            {
                modelSetsToGenerate ??= new();
                modelSetsToGenerate.Add(assetPath);
            }
            
            if (Path.GetExtension(assetPath) != ".model")
            {
                return;
            }

            var directory = new FileInfo(assetPath).Directory;
            while (directory != null && directory.Exists && directory.Name != "Assets")
            {
                var viewModelFiles = directory.GetFiles("*.viewmodel", SearchOption.TopDirectoryOnly);
                if (viewModelFiles.Length == 1)
                {
                    modelSetsToGenerate ??= new();
                    modelSetsToGenerate.Add(viewModelFiles[0].FullName);
                    break;
                }

                directory = directory.Parent;
            }
        }
        
        static string GetAssetsRelativePath (string absolutePath)
        {
            absolutePath = absolutePath.Replace("\\", "/");
            if (absolutePath.StartsWith(Application.dataPath)) {
                return "Assets" + absolutePath.Substring(Application.dataPath.Length);
            }
            
            throw new System.ArgumentException("Full path does not contain the current project's Assets folder;\n" + absolutePath+"\n"+Application.dataPath, "absolutePath");
        }
    }
}