using System.Collections.Generic;

namespace Uzi.Modeling.Editor
{
    public class ModelInnerClassDefinition
    {
        public string Name;
        public ModelClassDefinition ClassDefinition;
        public List<ModelAttribute> Attributes = new();
    }
}