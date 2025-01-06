using System.Collections.Generic;

namespace Uzi.Modeling.Editor
{
    public class ModelInnerEnumDefinition
    {
        public string Name;
        public ModelEnumDefinition EnumDefinition;
        public List<ModelAttribute> Attributes = new();
    }
}