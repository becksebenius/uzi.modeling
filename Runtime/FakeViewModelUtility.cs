using System;
using System.Reflection;

namespace Uzi.Modeling.Runtime
{
#if UNITY_EDITOR
    public static class FakeViewModelUtility
    {
        public static void RefreshModel(Type type)
        {
            var genericType = typeof(FakeViewModel<>).MakeGenericType(type);
            genericType.GetMethod("Refresh", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
        }

        public static bool HasFake(Type type)
        {
            var genericType = typeof(FakeViewModel<>).MakeGenericType(type);
            if (genericType.GetProperty("Exists", BindingFlags.Static | BindingFlags.Public)?.GetValue(null) is bool value)
            {
                return value;
            }

            return false;
        }

        public static object GetModel(Type type)
        {
            var genericType = typeof(FakeViewModel<>).MakeGenericType(type);
            return genericType.GetField("Value", BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
        }
    }
#endif
}