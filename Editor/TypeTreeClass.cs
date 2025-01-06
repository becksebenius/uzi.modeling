using System.Collections.Generic;

namespace Uzi.Modeling.Editor
{
    public class TypeTreeClass
    {
        public string Name;
        public List<TypeTreeEnum> Enums;
        public List<TypeTreeClass> Classes;
        
        public ModelClassDefinition ClassDefinition;
    }
}