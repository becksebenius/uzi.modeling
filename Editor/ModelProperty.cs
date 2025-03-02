using System.Collections.Generic;

namespace Uzi.Modeling.Editor
{
    public class ModelProperty
    {
        public string Name;
        public ModelPropertyType Type;
        public string ClassName;
        public ModelEnumDefinition InlineEnumDefinition;
        public ModelClassDefinition InlineClassDefinition;
        public List<ModelAttribute> Attributes = new ();
    }
}