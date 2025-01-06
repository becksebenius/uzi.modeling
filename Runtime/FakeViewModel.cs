using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Uzi;
using Uzi.Serialization;

namespace Uzi.Modeling.Runtime
{
    public static class FakeViewModel<T> where T : ModelBase<T>, new()
    {
        public static bool Exists { get; private set; }
        public static T Value;
#if UNITY_EDITOR
        static FakeViewModel()
        {
            try
            {
                Exists = false;
                Refresh();
            }
            catch (Exception e)
            {
                Logging.Exception(LogCategory.Editor, e);
            }
        }

        public static void Refresh()
        {
            Value ??= new T();

            var sourceDirectoryProperty = typeof(T).GetProperty("SourceDirectory", BindingFlags.Static | BindingFlags.NonPublic);
            if (sourceDirectoryProperty == null)
            {
                Logging.Error(LogCategory.Editor, "Invalid fake model type: " + typeof(T).Name);
                Exists = false;
                return;
            }

            var sourceDirectoryPath = (string)sourceDirectoryProperty.GetValue(null);
            var sourceDirectory = new DirectoryInfo(sourceDirectoryPath);
            
            var fakeFileName = $"_Fake_{typeof(T).Name}.json";
            var matchingFiles = sourceDirectory.GetFiles(fakeFileName, SearchOption.AllDirectories);
            if (!matchingFiles.Any())
            {
                Exists = false;
                return;
            }

            var file = matchingFiles.First();
            var json = File.ReadAllText(file.FullName);
            
            var deserializer = new JSONDeserializer();
            deserializer.Deserialize(Value, json);
            
            Value.InvokeModelUpdatedCallbacks(true);
            Exists = true;

            // Uncomment to test that it's deserializing properly
            // UnityEngine.Debug.Log(new JSONSerializer(new JSONSerializationParameters{ SkipNull = true, Format = true }).Serialize(Value));
        }
#endif
    }
}