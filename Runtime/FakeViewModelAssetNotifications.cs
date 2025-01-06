using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Uzi.Modeling.Runtime
{
#if UNITY_EDITOR
    public class FakeViewModelAssetNotifications : UnityEditor.AssetPostprocessor
    {
        public static event Action OnAnyFakeFileChanged;
        
        static bool IsFakeFile(string path, out Type fakedType)
        {
            fakedType = null;

            if (Path.GetExtension(path) != ".json")
            {
                return false;
            }

            var name = Path.GetFileNameWithoutExtension(path);
            if (!name.StartsWith("_Fake_"))
            {
                return false;
            }

            var fakedTypeName = name.Substring("_Fake_".Length);

            var assemblyName = UnityEditor.Compilation.CompilationPipeline.GetAssemblyNameFromScriptPath(path);
            if (assemblyName == null)
            {
                return false;
            }

            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => (a.GetName().Name+".dll").Equals(assemblyName));
            if (assembly == null)
            {
                return false;
            }
            
            var type = assembly.GetTypes().FirstOrDefault(t => t.Name.Equals(fakedTypeName));
            if (type == null)
            {
                return false;
            }

            var sourceDirectoryPath = type.GetProperty("SourceDirectory", BindingFlags.Static | BindingFlags.NonPublic);
            if (sourceDirectoryPath == null)
            {
                return false;
            }

            fakedType = type;
            return true;
        }
        
        static void OnPostprocessAllAssets(
            string[] importedAssets, 
            string[] deletedAssets, 
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            RefreshFakes(importedAssets);
            RefreshFakes(deletedAssets);
            RefreshFakes(movedAssets);
            RefreshFakes(movedFromAssetPaths);
        }

        static void RefreshFakes(string[] assets)
        {
            foreach (var asset in assets)
            {
                if (!IsFakeFile(asset, out var type))
                {
                    continue;
                }
                
                FakeViewModelUtility.RefreshModel(type);
                
                OnAnyFakeFileChanged?.Invoke();
            }
        }
    }
#endif
}