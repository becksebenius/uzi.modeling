using System.Collections.Generic;

namespace Uzi.Modeling.Editor
{
    public class ModelClassDefinition
    {
        public readonly List<ModelInnerEnumDefinition> InnerEnums = new();
        public readonly List<ModelInnerClassDefinition> InnerClasses = new();
        public readonly List<ModelProperty> Properties = new();
    }
}