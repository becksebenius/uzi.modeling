using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Uzi.Modeling.Editor
{
    public static class ModelCodeGeneration
    {
        class GeneratedFile
        {
            public string Destination;
            public string Contents;
        }

        class ImportedModelFile
        {
            public string SourceFilePath;
            public string DestinationPath;
            public ModelFile FileData;
        }
        
        public static void Generate(
            string directory,
            ModelSetConfiguration configuration)
        {
            var outputDirectoryPath = Path.Combine(directory, "Generated");
            
            List<GeneratedFile> generatedFiles = new List<GeneratedFile>();

            List<ImportedModelFile> importedFiles = new List<ImportedModelFile>();
            foreach (var file in Directory.GetFiles(directory, "*.model", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);
                var modelFile = ModelFileParser.Parse(text, configuration.TypeMappings);
                
                var relativePath = Path.GetRelativePath(directory, file);
                relativePath = Path.ChangeExtension(relativePath, ".cs");
                var destination = Path.Combine(outputDirectoryPath, relativePath);
                
                importedFiles.Add(new ImportedModelFile()
                {
                    SourceFilePath = file,
                    DestinationPath = destination,
                    FileData = modelFile
                });
            }

            var typeTree = TypeTree.FromModelFiles(importedFiles.Select(f => f.FileData));
            
            foreach (var file in importedFiles)
            {
                var outputFile = GenerateFile(
                    typeTree,
                    file.FileData, 
                    configuration,
                    directory);
                generatedFiles.Add(new GeneratedFile()
                {
                    Destination = file.DestinationPath,
                    Contents = outputFile
                });
            }

            if (!Directory.Exists(outputDirectoryPath))
            {
                Directory.CreateDirectory(outputDirectoryPath);
            }
            
            foreach (var file in Directory.GetFiles(outputDirectoryPath, "*.*"))
            {
                File.Delete(file);
            }

            foreach (var file in generatedFiles)
            {
                var fileInfo = new FileInfo(file.Destination);
                if (!fileInfo.Directory.Exists)
                {
                    fileInfo.Directory.Create();
                }
                File.WriteAllText(file.Destination, file.Contents);
            }
            
            AssetDatabase.Refresh();
        }
        
        static class Templates
        {
            public const string File = 
@"/* GENERATED FILE, DO NOT MODIFY */
using System;
using Uzi.Modeling.Runtime;
using UnityEngine;

namespace {Namespace}
{
{Body}
}
";
            public const string Class = 
@"[Serializable]
public class {ClassName} : ModelBase<{ClassName}>{Interfaces}
{
{Body}
}";

            public const string ForceMarkDirty =
@"protected override void ForceMarkDirty()
{
    base.ForceMarkDirty();
{Body}
}";

            public const string InvokeModelUpdatedCallbacksOnChildren =
@"protected override void InvokeModelUpdatedCallbacksOnChildren(bool force, Action<Exception, object> onExceptionCallback)
{
{Body}
}";
            
            public const string PrimitiveProperty = 
@"[SerializeField] {PrivateTypeName} __{PropertyName};
public {TypeName} {PropertyName} 
{ 
    get => __{PropertyName}; 
    set
    {
        if(__{PropertyName} != value)
        {
            __{PropertyName} = value;
            SelfChanged();
            parent?.NotifyChildChanged();
        }
    }
}";
        
            public const string ObjectProperty =
@"[SerializeField] {PrivateTypeName} __{PropertyName};
public {TypeName} {PropertyName}
{
    get => __{PropertyName};
    set
    {
        if(value == null)
        {
            throw new ArgumentNullException(nameof({PropertyName}));
        }
        __{PropertyName}.TransferInternals(value);
        __{PropertyName} = value;
        SelfChanged();
        parent?.NotifyChildChanged();
    }
}";
            
            public const string ListProperty =
@"[SerializeField] {PrivateTypeName} __{PropertyName};
public {TypeName} {PropertyName} => __{PropertyName};";

            public const string Constructor =
@"public {ClassName}()
{
{Body}
}";

            public const string LocatorParentClass =
@"
public static partial class Locators
{
{Body}
}
";
            
            public const string LocatorClass =
@"public class {ClassName} : ModelLocator<{RootModelTypeName},{ReferenceClass}>
{
{Body}
    public {ClassName}() : base(model => model{LocatorPath}){}
}";

            public const string LocatorRoot =
@"public static partial class {ClassName}
{
{Body}
}";
            
            public const string LocatorSubClass =
@"public static class {ClassName}
{
{Body}
}";

            public const string LocatorField =
@"public readonly {ClassName} {PropertyName} = new ();";
            
            public const string LocatorFieldGeneric =
@"public readonly ModelLocator<{RootModelTypeName},{ClassName}> {PropertyName} = new (model => model.{LocatorPath});";

            public const string LocatorFieldList =
@"public readonly ListModelLocator<{RootModelTypeName},{ClassName},{EntryClassName}> {PropertyName} = new (model => model.{LocatorPath});";
            
            public const string LocatorRootField =
@"public static readonly {ClassName} {PropertyName} = new ();";
            
            public const string LocatorReferenceGroupClass =
@"public static class {ClassName}
{
{Body}
}";

            public const string Enum =
@"public enum {ClassName}
{
{Body}
}";

            public const string SourceDirectory =
@"#if UNITY_EDITOR
static string SourceDirectory => {SourceDirectory};
#endif";
        }

        static string GenerateFile(
            TypeTree typeTree,
            ModelFile modelFile, 
            ModelSetConfiguration configuration,
            string sourceDirectory)
        {
            var body = new StringBuilder();
            
            foreach (var innerEnum in modelFile.Enums)
            {
                var enumDef = GenerateEnum(innerEnum.Name, innerEnum.EnumDefinition, innerEnum.Attributes);
                body.Append(enumDef);
                body.AppendLine();
            }
            
            foreach (var innerClass in modelFile.Classes)
            {
                var generatedClassDefinition = GenerateClass(
                    innerClass.Name, 
                    innerClass.ClassDefinition, 
                    innerClass.Attributes, 
                    1, 
                    innerClass.Attributes.Any(a => a.AttributeName.Equals("BindTarget")),
                    sourceDirectory);
                body.Append(generatedClassDefinition);
                body.AppendLine();
            }
            
            var locatorClasses = new StringBuilder();
            var locatorRootClassBody = new StringBuilder();
            
            foreach (var innerClass in modelFile.Classes)
            {
                List<string> typePath = new List<string>();
                var result = GenerateLocators(
                    innerClass.Name,
                    innerClass.ClassDefinition,
                    innerClass.Attributes,
                    typeTree,
                    typePath);

                locatorClasses.Append(result.LocatorClassDefinitions);
                locatorRootClassBody.Append(result.LocatorReferences);
            }

            if (0 < locatorClasses.Length
            || 0 < locatorRootClassBody.Length)
            {
                Indent(locatorClasses, 1);
                Indent(locatorRootClassBody, 1);

                body.Append(Templates.LocatorParentClass
                    .Replace("{Body}", locatorClasses.ToString()));
                body.AppendLine();
                body.Append(Templates.LocatorRoot
                    .Replace("{Body}", locatorRootClassBody.ToString().TrimEnd())
                    .Replace("{ClassName}", configuration.LocatorsClassName));
                body.AppendLine();
            }

            Indent(body, 1);

            var file = Templates.File
                .Replace("{Body}", body.ToString())
                .Replace("{Namespace}", configuration.Namespace);
            
            file = Regex.Replace(file, @"\r\n|\n\r|\n|\r", "\r\n");
            
            return file;
        }

        static string GenerateClass(
            string className, 
            ModelClassDefinition definition,
            List<ModelAttribute> attributes,
            int indentLevel,
            bool isBindTarget,
            string sourceDirectory)
        {
            StringBuilder constructorBody = new StringBuilder();
            StringBuilder callbackInvocationBody = new StringBuilder();
            StringBuilder forceMarkDirtyBody = new StringBuilder();
            StringBuilder sourceFileImplementation = new StringBuilder();
            StringBuilder interfaces = new StringBuilder();

            if (isBindTarget)
            {
                interfaces.Append(", IBindTarget");
            }

            foreach (var attribute in attributes)
            {
                const string interfacePrefix = "Interface:";
                if (attribute.AttributeName.StartsWith(interfacePrefix))
                {
                    var interfaceName = attribute.AttributeName.Substring(interfacePrefix.Length);
                    interfaces.Append(", " + interfaceName);
                }
            }

            if (sourceDirectory != null)
            {
                sourceFileImplementation.Append(
                    Templates.SourceDirectory
                        .Replace("{SourceDirectory}", ToLiteral(sourceDirectory)));
            }
            
            StringBuilder classes = new StringBuilder();
            foreach (var innerEnum in definition.InnerEnums)
            {
                var enumDef = GenerateEnum(innerEnum.Name, innerEnum.EnumDefinition, innerEnum.Attributes);
                classes.Append(enumDef);
                classes.AppendLine();
            }
            
            foreach (var innerType in definition.InnerClasses)
            {
                var innerClass = GenerateClass(innerType.Name, innerType.ClassDefinition, innerType.Attributes, 1, false, null);
                classes.Append(innerClass);
                classes.AppendLine();
            }
            foreach (var property in definition.Properties)
            {
                if (property.InlineClassDefinition != null)
                {
                    var innerClass = GenerateClass(property.ClassName, property.InlineClassDefinition, property.Attributes, 1, false, null);
                    classes.Append(innerClass);
                    classes.AppendLine();
                }

                if (property.InlineEnumDefinition != null)
                {
                    var innerEnum = GenerateEnum(property.ClassName, property.InlineEnumDefinition, property.Attributes);
                    classes.Append(innerEnum);
                    classes.AppendLine();
                }
            }
            Indent(classes, indentLevel);
            
            StringBuilder properties = new StringBuilder();
            foreach (var property in definition.Properties)
            {
                var propertyBody =
                    property.Type == ModelPropertyType.Object
                    ? Templates.ObjectProperty
                    : property.Type == ModelPropertyType.List
                    ? Templates.ListProperty
                    : Templates.PrimitiveProperty;

                var typeName = GetClassName(property);
                var privateTypeName = typeName;
                propertyBody = propertyBody.Replace("{TypeName}", typeName);
                propertyBody = propertyBody.Replace("{PropertyName}", property.Name);
                
                if (property.Type == ModelPropertyType.Object)
                {
                    constructorBody.AppendLine("__" + property.Name + " = new() { parent = this };");
                    callbackInvocationBody.AppendLine("__" + property.Name + ".InvokeModelUpdatedCallbacks(force, onExceptionCallback);");
                    forceMarkDirtyBody.AppendLine("((IModelInternal)__" + property.Name + ").ForceMarkDirty();");
                }
                else if (property.Type == ModelPropertyType.List)
                {
                    constructorBody.AppendLine("__" + property.Name + " = new ModelList<" + property.ClassName + ">(this, (obj, p) => obj.parent = p);");
                    privateTypeName = "ModelList<" + property.ClassName + ">";
                    callbackInvocationBody.AppendLine("__" + property.Name + ".InvokeModelUpdatedCallbacks(force, onExceptionCallback);");
                    forceMarkDirtyBody.AppendLine("((IModelInternal)__" + property.Name + ").ForceMarkDirty();");
                }
                
                propertyBody = propertyBody.Replace("{PrivateTypeName}", privateTypeName);

                properties.Append(propertyBody);
                properties.AppendLine();
                properties.AppendLine();
            }
            
            Indent(properties, indentLevel);
            
            var invokeModelUpdatedCallbacksOnChildren = new StringBuilder();
            if (0 < callbackInvocationBody.Length)
            {
                Indent(callbackInvocationBody, 1);
                invokeModelUpdatedCallbacksOnChildren.Append(Templates.InvokeModelUpdatedCallbacksOnChildren
                    .Replace("{Body}", callbackInvocationBody.ToString().TrimEnd()));
                Indent(invokeModelUpdatedCallbacksOnChildren, indentLevel);
            }

            var forceMarkDirty = new StringBuilder();
            if (0 < forceMarkDirtyBody.Length)
            {
                Indent(forceMarkDirtyBody, 1);
                forceMarkDirty.AppendLine(Templates.ForceMarkDirty
                    .Replace("{Body}", forceMarkDirtyBody.ToString().TrimEnd()));
                Indent(forceMarkDirty, indentLevel);
            }
            
            var constructor = new StringBuilder();
            if (0 < constructorBody.Length)
            {
                Indent(constructorBody, 1);
                constructor.Append(Templates.Constructor
                    .Replace("{ClassName}", className)
                    .Replace("{Body}", constructorBody.ToString().TrimEnd()));
                Indent(constructor, indentLevel);
            }

            if (0 < sourceFileImplementation.Length)
            {
                Indent(sourceFileImplementation, indentLevel);
            }

            return Templates.Class
                .Replace("{ClassName}", className)
                .Replace("{Interfaces}", interfaces.ToString())
                .Replace("{Body}", CombineBodySection(
                    sourceFileImplementation,
                    classes, 
                    properties, 
                    constructor, 
                    forceMarkDirty,
                    invokeModelUpdatedCallbacksOnChildren));
        }

        static string GenerateEnum(
            string className,
            ModelEnumDefinition definition,
            List<ModelAttribute> attributes)
        {
            StringBuilder body = new StringBuilder();
            for(int i = 0; i < definition.Values.Count; ++i)
            {
                body.Append(definition.Values[i]);
                if (i != definition.Values.Count - 1)
                {
                    body.AppendLine(",");
                }
            }
            
            Indent(body, 1);

            return Templates.Enum
                .Replace("{ClassName}", className)
                .Replace("{Body}", body.ToString());
        }

        struct LocatorGenerationResult
        {
            public StringBuilder LocatorClassDefinitions;
            public StringBuilder LocatorReferences;
        }
        
        static LocatorGenerationResult GenerateLocators(
            string className,
            ModelClassDefinition classDefinition,
            List<ModelAttribute> attributes,
            TypeTree typeTree,
            List<string> typePath)
        {
            typePath.Add(className);
            
            var locatorClassDefinitions = new StringBuilder();
            var locatorReferences = new StringBuilder();

            var nestedClassDefinitions = new List<string>();
            var nestedReferences = new List<string>();
            
            foreach (var innerClass in classDefinition.InnerClasses)
            {
                var result = GenerateLocators(
                    innerClass.Name,
                    innerClass.ClassDefinition,
                    innerClass.Attributes,
                    typeTree,
                    typePath);

                nestedClassDefinitions.Add(result.LocatorClassDefinitions.ToString().TrimEnd());
                nestedReferences.Add(result.LocatorReferences.ToString().TrimEnd());
            }

            foreach (var property in classDefinition.Properties)
            {
                if (property.InlineClassDefinition != null)
                {
                    var result = GenerateLocators(
                        property.ClassName,
                        property.InlineClassDefinition,
                        property.Attributes,
                        typeTree,
                        typePath);

                    nestedClassDefinitions.Add(result.LocatorClassDefinitions.ToString().TrimEnd());
                    nestedReferences.Add(result.LocatorReferences.ToString().TrimEnd());
                }
            }

            if (nestedClassDefinitions.Any(s => !string.IsNullOrEmpty(s.Trim()))
            || nestedReferences.Any(s => !string.IsNullOrEmpty(s.Trim())))
            {
                var nestedClassDefinitionsCombined = new StringBuilder(
                    CombineBodySection(nestedClassDefinitions.Select(s => new StringBuilder(s)).ToArray()));
                var nestedReferencesCombined = new StringBuilder(
                    CombineBodySection(nestedReferences.Select(s => new StringBuilder(s)).ToArray()));
                Indent(nestedClassDefinitionsCombined, 1);
                Indent(nestedReferencesCombined, 1);

                locatorClassDefinitions.AppendLine(Templates.LocatorSubClass
                    .Replace("{ClassName}", className+"Locators")
                    .Replace("{Body}", nestedClassDefinitionsCombined.ToString()));

                locatorReferences.AppendLine(Templates.LocatorReferenceGroupClass
                    .Replace("{ClassName}", className+"BindTargets")
                    .Replace("{Body}", nestedReferencesCombined.ToString()));
            }
            
            if (attributes.Any(a => a.AttributeName.Equals("BindTarget")))
            {
                locatorClassDefinitions.AppendLine(
                    GenerateLocatorClass(
                        typePath.ContentsToString('.'),
                        typeTree,
                        typePath,
                        classDefinition,
                        className + "Locator",
                        typePath.ContentsToString('.'),
                        string.Empty));

                locatorReferences.AppendLine(
                    Templates.LocatorRootField
                        .Replace("{ClassName}", "Locators." + GetLocatorClassReference(typePath) + "Locator")
                        .Replace("{PropertyName}", className));
            }
            
            typePath.Pop();

            return new LocatorGenerationResult
            {
                LocatorClassDefinitions = locatorClassDefinitions,
                LocatorReferences = locatorReferences
            };
        }

        static string GenerateLocatorClass(
            string rootModelTypeName,
            TypeTree typeTree,
            List<string> typeTreePath,
            ModelClassDefinition modelClassDefinition,
            string className,
            string referenceClassPath,
            string locatorPath)
        {
            StringBuilder locatorClasses = new StringBuilder();
            StringBuilder locatorProperties = new StringBuilder();

            foreach (var property in modelClassDefinition.Properties)
            {
                if (property.Type == ModelPropertyType.Object
                || property.Type == ModelPropertyType.List)
                {
                    ModelClassDefinition classDefinition;
                    string locatorReferenceClassPath;
                    List<string> classPath;
                    if (property.InlineClassDefinition != null)
                    {
                        classDefinition = property.InlineClassDefinition;
                        locatorReferenceClassPath = referenceClassPath + "." + property.ClassName;
                        classPath = typeTreePath.Clone();
                        classPath.Add(property.ClassName);
                    }
                    else if (!TryFindTypeForLocator(
                        typeTree,
                        typeTreePath,
                        property.ClassName,
                        out classDefinition,
                        out locatorReferenceClassPath,
                        out classPath))
                    {
                        throw new Exception("Failed to find reference to class type " + property.ClassName + " (Type Tree Path: " + typeTreePath.ContentsToString('.') + ")");
                    }

                    string subLocatorPath;
                    if (string.IsNullOrEmpty(locatorPath))
                    {
                        subLocatorPath = property.Name;
                    }
                    else
                    {
                        subLocatorPath = locatorPath + "." + property.Name;
                    }

                    if (property.Type == ModelPropertyType.Object)
                    {
                        if (AnyPropertiesNeedLocators(classDefinition))
                        {
                            var locatorClass = GenerateLocatorClass(
                                rootModelTypeName,  
                                typeTree,
                                classPath,
                                classDefinition,
                                property.Name+"Locator",
                                locatorReferenceClassPath,
                                subLocatorPath);
                            locatorClasses.Append(locatorClass);
                            locatorClasses.AppendLine();

                            locatorProperties.AppendLine(Templates.LocatorField
                                .Replace("{ClassName}", property.Name+"Locator")
                                .Replace("{PropertyName}", property.Name));
                        }
                        else
                        {
                            locatorProperties.AppendLine(Templates.LocatorFieldGeneric)
                                .Replace("{ClassName}", locatorReferenceClassPath)
                                .Replace("{PropertyName}", property.Name)
                                .Replace("{RootModelTypeName}", rootModelTypeName)
                                .Replace("{LocatorPath}", subLocatorPath);
                        }
                    }
                    else if (property.Type == ModelPropertyType.List)
                    {
                        locatorProperties.AppendLine(Templates.LocatorFieldList)
                            .Replace("{ClassName}", "IModelList<" + locatorReferenceClassPath + ">")
                            .Replace("{EntryClassName}", locatorReferenceClassPath)
                            .Replace("{PropertyName}", property.Name)
                            .Replace("{RootModelTypeName}", rootModelTypeName)
                            .Replace("{LocatorPath}", subLocatorPath);
                    }
                }
            }
            
            Indent(locatorClasses, 1);
            Indent(locatorProperties, 1);
            
            var classBody = Templates.LocatorClass
                .Replace("{Body}", CombineBodySection(locatorClasses, locatorProperties))
                .Replace("{ClassName}", className)
                .Replace("{RootModelTypeName}", rootModelTypeName)
                .Replace("{ReferenceClass}", referenceClassPath)
                .Replace("{LocatorPath}", string.IsNullOrEmpty(locatorPath) ? string.Empty : "." + locatorPath);

            return classBody;
        }

        static string GetLocatorClassReference(List<string> typePath)
        {
            string result = string.Empty;
            for (int i = 0; i < typePath.Count - 1; ++i)
            {
                result += typePath[i] + "Locators.";
            }

            result += typePath[^1];
            return result;
        }

        static string CombineBodySection(params StringBuilder[] builders)
        {
            string result = string.Empty;
            foreach (var builder in builders)
            {
                var value = builder.ToString().TrimEnd();
                if (string.IsNullOrEmpty(value.Trim()))
                {
                    continue;
                }
                
                if (result != string.Empty)
                {
                    result += "\n\n";
                }

                result += value;
            }
            return result;
        }
        
        static void Indent(StringBuilder builder, int indentLevel)
        {
            var tabChars = new char[indentLevel * 4];
            for(int i = 0; i < tabChars.Length; ++i)
            {
                tabChars[i] = ' ';
            }

            var tabStr = new string(tabChars);
            
            for (int i = 0; i < builder.Length; ++i)
            {
                if (builder[i] == '\n')
                {
                    builder.Insert(i+1, tabStr);
                    i += tabStr.Length;
                }
            }

            builder.Insert(0, tabStr);
        }

        static bool AnyPropertiesNeedLocators(ModelClassDefinition definition)
        {
            foreach (var property in definition.Properties)
            {
                if (property.Type == ModelPropertyType.Object
                || property.Type == ModelPropertyType.List)
                {
                    return true;
                }
            }

            return false;
        }

        static bool TryFindTypeForLocator(
            TypeTree typeTree,
            List<string> typeTreePath,
            string query,
            out ModelClassDefinition classDefinition,
            out string referenceClass,
            out List<string> classPath)
        {
            for (int i = typeTreePath.Count - 1; 0 <= i; --i)
            {
                if (!TryGetTypeTreeClass(typeTree, typeTreePath, i, out var typeTreeClass))
                {
                    UnityEngine.Debug.LogError("Failed to find class at path " + typeTreePath.ContentsToString('.'));
                    break;
                }

                var queriedClass = typeTreeClass.Classes?.FirstOrDefault(c => c.Name.Equals(query));
                if (queriedClass != null)
                {
                    classDefinition = queriedClass.ClassDefinition;

                    classPath = new List<string>();
                    referenceClass = string.Empty;
                    for (int j = 0; j < i; ++j)
                    {
                        classPath.Add(typeTreePath[j]);
                        referenceClass += typeTreePath[j] + ".";
                    }
                    referenceClass += query;
                    classPath.Add(query);

                    return true;
                }
            }
            
            classDefinition = null;
            referenceClass = null;
            classPath = null;
            return false;
        }

        static bool TryGetTypeTreeClass(
            TypeTree typeTree, 
            List<string> typeTreePath, 
            int depth,
            out TypeTreeClass outClass)
        {
            TypeTreeClass current = typeTree;
            for (int i = 0; i < depth; ++i)
            {
                var next = current.Classes?.FirstOrDefault(c => c.Name.Equals(typeTreePath[i]));
                if (next == null)
                {
                    outClass = default;
                    return false;
                }

                current = next;
            }

            outClass = current;
            return true;
        }

        static string GetClassName(ModelProperty property)
        {
            if (property.Type == ModelPropertyType.Object)
            {
                return property.ClassName;
            }
            else if (property.Type == ModelPropertyType.List)
            {
                return "IModelList<" + property.ClassName + ">";
            }
            else if (property.Type == ModelPropertyType.Action)
            {
                if (!string.IsNullOrEmpty(property.ClassName))
                {
                    return "Action<" + property.ClassName + ">";
                }
                else
                {
                    return "Action";
                }
            }
            else if (property.Type == ModelPropertyType.Enum)
            {
                return property.ClassName;
            }
            else if (property.Type == ModelPropertyType.External)
            {
                return property.ClassName;
            }
            else
            {
                switch (property.Type)
                {
                    case ModelPropertyType.Bool:
                        return "bool";
                    case ModelPropertyType.Float:
                        return "float";
                    case ModelPropertyType.Int:
                        return "int";
                    case ModelPropertyType.String:
                        return "string";
                }

                throw new NotSupportedException(property.Type.ToString());
            }
        }
        
        static string ToLiteral(string input)
        {
            using (var writer = new StringWriter())
            {
                using (var provider = CodeDomProvider.CreateProvider("CSharp"))
                {
                    provider.GenerateCodeFromExpression(new CodePrimitiveExpression(input), writer, null);
                    return writer.ToString();
                }
            }
        }
    }
}