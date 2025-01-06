using System.Collections.Generic;

namespace Uzi.Modeling.Editor
{
    public class ModelSetConfiguration
    {
        public string Namespace;
        public string LocatorsClassName;
        public Dictionary<string,string> TypeMappings = new();
    }
}