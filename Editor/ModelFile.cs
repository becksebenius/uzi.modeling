using System.Collections.Generic;

namespace Uzi.Modeling.Editor
{
    public class ModelFile
    {    
        public readonly List<ModelInnerEnumDefinition> Enums = new();
        public readonly List<ModelInnerClassDefinition> Classes = new();
    }
}