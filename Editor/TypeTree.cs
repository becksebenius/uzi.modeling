using System.Collections.Generic;

namespace Uzi.Modeling.Editor
{
    public class TypeTree : TypeTreeClass
    {
        public static TypeTree FromModelFiles(IEnumerable<ModelFile> files)
        {
            var typeTree = new TypeTree();
            foreach (var file in files)
            {
                PopulateTypeTreeClassRecursive(
                    typeTree,
                    null,
                    file.Classes,
                    file.Enums);
            }

            return typeTree;
        }

        static void PopulateTypeTreeClassRecursive(
            TypeTreeClass typeTreeClass,
            List<ModelProperty> properties,
            List<ModelInnerClassDefinition> classes,
            List<ModelInnerEnumDefinition> enums)
        {
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    if (property.InlineClassDefinition != null)
                    {
                        var innerClass = new TypeTreeClass
                        {
                            Name = property.ClassName,
                            ClassDefinition = property.InlineClassDefinition
                        };
                        PopulateTypeTreeClassRecursive(
                            innerClass,
                            property.InlineClassDefinition.Properties,
                            property.InlineClassDefinition.InnerClasses,
                            property.InlineClassDefinition.InnerEnums);

                        typeTreeClass.Classes ??= new List<TypeTreeClass>();
                        typeTreeClass.Classes.Add(innerClass);
                    }

                    if (property.InlineEnumDefinition != null)
                    {
                        typeTreeClass.Enums ??= new List<TypeTreeEnum>();
                        typeTreeClass.Enums.Add(new TypeTreeEnum
                        {
                            Name = property.ClassName
                        });
                    }
                }
            }

            foreach (var type in classes)
            {
                var innerClass = new TypeTreeClass()
                {
                    Name = type.Name,
                    ClassDefinition = type.ClassDefinition
                };
                PopulateTypeTreeClassRecursive(
                    innerClass,
                    type.ClassDefinition.Properties,
                    type.ClassDefinition.InnerClasses,
                    type.ClassDefinition.InnerEnums);
                typeTreeClass.Classes ??= new List<TypeTreeClass>();
                typeTreeClass.Classes.Add(innerClass);
            }

            foreach (var innerEnum in enums)
            {
                typeTreeClass.Enums ??= new List<TypeTreeEnum>();
                typeTreeClass.Enums.Add(new TypeTreeEnum
                {
                    Name = innerEnum.Name
                });
            }
        }
    }
}