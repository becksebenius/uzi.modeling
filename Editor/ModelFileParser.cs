using System;
using System.Collections.Generic;
using System.Linq;
using Uzi;

namespace Uzi.Modeling.Editor
{
    public struct ModelFileParser
    {
        static readonly char[] WhitespaceChars = {
            (char)32, '\n', '\r', '\t'
        };

        static readonly char[] FieldBreakChars = {
            ';', '{'
        };

        static readonly char[] PostBlockSkipChars = new[]
        {
            ';'
        }.Union(WhitespaceChars).ToArray();

        public static ModelFile Parse(string input, Dictionary<string,string> typeMappings)
        {
            return new ModelFileParser
            {
                input = input,
                typeMappings = typeMappings
            }.ParseFile();
        }
        
        string input;
        Dictionary<string,string> typeMappings;

        ModelFile ParseFile()
        {
            var modelFile = new ModelFile();

            var objectType = ParseObjectType(0, input.Length);
            modelFile.Classes.AddRange(objectType.InnerClasses);
            modelFile.Enums.AddRange(objectType.InnerEnums);
            
            foreach (var property in objectType.Properties)
            {
                throw new Exception(
                    $"Parsing error: Properties cannot be defined at the root level of the file.");
            }

            return modelFile;
        }

        ModelClassDefinition ParseObjectType(
            int blockStart,
            int blockEnd)
        {
            var typeDefinition = new ModelClassDefinition();
            
            int cursor = blockStart;
            while (cursor < blockEnd)
            {
                int start = cursor;
                SkipWhitespace(ref cursor);

                if (input[cursor] == '}')
                {
                    break;
                }

                SkipComments(ref cursor);

                var attributes = new List<ModelAttribute>();
                while(input[cursor] == '@')
                {
                    var attributeStart = cursor+1;
                    var attributeEnd = attributeStart;
                    SkipUntilWhitespace(ref attributeEnd);
                    var attributeName = input.Substring(attributeStart, attributeEnd - attributeStart);
                    attributes.Add(new ModelAttribute()
                    {
                        AttributeName = attributeName
                    });
                    cursor = attributeEnd;
                    SkipWhitespace(ref cursor);
                }
                
                SkipComments(ref cursor);
                
                if (input[cursor] == '}')
                {
                    break;
                }
                
                if (!IsAlpha(input[cursor]))
                {
                    throw new Exception(
                        $"Parsing error: Expected member but character '{input[cursor]}' is not a valid character.");
                }

                int memberTypeNameStart = cursor;
                int memberTypeNameEnd = memberTypeNameStart;
                SkipUntilWhitespace(ref memberTypeNameEnd);
                
                int memberNameStart = memberTypeNameEnd + 1;
                SkipWhitespace(ref memberNameStart);
                
                int memberBreak = memberNameStart;
                SkipUntilAny(ref memberBreak, FieldBreakChars);
                
                int memberNameEnd = memberBreak;
                if (IsWhitespace(input[memberNameEnd-1]))
                {
                    memberNameEnd--;
                    BacktrackWhitespace(ref memberNameEnd);
                    memberNameEnd++;
                }

                var memberTypeName = input.Substring(memberTypeNameStart, memberTypeNameEnd - memberTypeNameStart);

                string memberName;
                if (memberNameEnd - memberNameStart <= 0)
                {
                    memberName = memberTypeName;
                    memberTypeName = memberTypeName + "Model";
                }
                else
                {
                    memberName = input.Substring(memberNameStart, memberNameEnd - memberNameStart);
                }

                if (!CheckIsValidMemberName(memberName))
                {
                    throw new Exception("Parsing error: Invalid member name: " + memberName);
                }

                bool hasInnerBlock = false;
                int innerBlockStart = -1;
                int innerBlockEnd = -1;
                
                if (input[memberBreak] == '{')
                {
                    innerBlockStart = memberBreak;
                    innerBlockEnd = GetBlockEnd(innerBlockStart);

                    if (blockEnd <= innerBlockEnd)
                    {
                        throw new Exception("Parsing error: Missing end bracket for type definition " + (memberTypeName == "define" ? memberName : memberTypeName));
                    }

                    hasInnerBlock = true;
                    cursor = innerBlockEnd + 1;
                    SkipPast(ref cursor, PostBlockSkipChars);
                }
                else if (input[memberBreak] == ';')
                {
                    cursor = memberBreak + 1;
                }
                else
                {
                    throw new Exception("Parsing error: Invalid terminator on field");
                }
                
                if (memberTypeName.Equals("enum"))
                {
                    if (!CheckIsValidMemberName(memberNameStart, memberNameEnd))
                    {
                        throw new Exception("Parsing error: Invalid type name: " + memberName);
                    }

                    if (!hasInnerBlock)
                    {
                        throw new Exception("Parsing error: Missing type definition for inner type " + memberName);
                    }
                    
                    typeDefinition.InnerEnums.Add(new ModelInnerEnumDefinition
                    {
                        Name = memberName,
                        EnumDefinition = ParseEnum(innerBlockStart+1, innerBlockEnd),
                        Attributes = attributes 
                    });
                }
                else if (memberTypeName.Equals("define"))
                {
                    if (!CheckIsValidMemberName(memberNameStart, memberNameEnd))
                    {
                        throw new Exception("Parsing error: Invalid type name: " + memberName);
                    }

                    if (!hasInnerBlock)
                    {
                        throw new Exception("Parsing error: Missing type definition for inner type " + memberName);
                    }
                    
                    typeDefinition.InnerClasses.Add(new ModelInnerClassDefinition
                    {
                        Name = memberName,
                        ClassDefinition = ParseObjectType(innerBlockStart+1, innerBlockEnd),
                        Attributes = attributes
                    });
                }
                else if (memberTypeName.EndsWith("[]"))
                {
                    var subType = memberTypeName.Substring(0, memberTypeName.Length - 2);
                    if (typeMappings.TryGetValue(subType, out var _))
                    {
                        throw new Exception("Parsing error: Cannot use an external type as an array.");
                    }

                    typeDefinition.Properties.Add(new ModelProperty
                    {
                        Name = memberName,
                        Attributes = attributes,
                        Type = ModelPropertyType.List,
                        ClassName = subType,
                        InlineClassDefinition = hasInnerBlock ? ParseObjectType(innerBlockStart+1, innerBlockEnd) : null
                    });
                }
                else if (memberTypeName.StartsWith("Action<"))
                {
                    if (memberTypeName[memberTypeName.Length - 1] != '>')
                    {
                        throw new Exception("Parsing error: Missing terminator on Action type name");
                    }
                    
                    var subType = memberTypeName.Substring(7, memberTypeName.Length - 8);
                    if (typeMappings.TryGetValue(subType, out var mappedType))
                    {
                        if (hasInnerBlock)
                        {
                            throw new Exception("Parsing error: Cannot define an inline block for an external type");
                        }
                        subType = mappedType;
                    }
                    typeDefinition.Properties.Add(new ModelProperty
                    {
                        Name = memberName,
                        Attributes = attributes,
                        Type = ModelPropertyType.Action,
                        ClassName = subType,
                        InlineClassDefinition = hasInnerBlock ? ParseObjectType(innerBlockStart+1, innerBlockEnd) : null
                    });
                }
                else if (memberTypeName.StartsWith("Enum<"))
                {
                    if (memberTypeName[memberTypeName.Length - 1] != '>')
                    {
                        throw new Exception("Parsing error: Missing terminator on Enum type name");
                    }
                    
                    var subType = memberTypeName.Substring(5, memberTypeName.Length - 6);
                    if (typeMappings.TryGetValue(subType, out var mappedType))
                    {
                        if (hasInnerBlock)
                        {
                            throw new Exception("Parsing error: Cannot define an inline block for an external type");
                        }
                        subType = mappedType;
                    }
                    
                    typeDefinition.Properties.Add(new ModelProperty
                    {
                        Name = memberName,
                        Attributes = attributes,
                        Type = ModelPropertyType.Enum,
                        ClassName = subType,
                        InlineEnumDefinition = hasInnerBlock ? ParseEnum(innerBlockStart+1, innerBlockEnd) : null
                    });
                }
                else if (Enum.TryParse(memberTypeName, true, out ModelPropertyType type))
                {
                    if (hasInnerBlock)
                    {
                        throw new Exception("Parsing error: Inline type invalid for primitive properties");
                    }

                    typeDefinition.Properties.Add(new ModelProperty
                    {
                        Name = memberName,
                        Attributes = attributes,
                        Type = type
                    });
                }
                else if (typeMappings.TryGetValue(memberTypeName, out var mappedType))
                {
                    typeDefinition.Properties.Add(new ModelProperty()
                    {
                        Name = memberName,
                        Attributes = attributes,
                        Type = ModelPropertyType.External,
                        ClassName = mappedType
                    });
                }
                else if (memberTypeName.Contains("."))
                {
                    typeDefinition.Properties.Add(new ModelProperty()
                    {
                        Name = memberName,
                        Attributes = attributes,
                        Type = ModelPropertyType.External,
                        ClassName = memberTypeName
                    });
                }
                else
                {
                    if (!CheckIsValidMemberName(memberTypeNameStart, memberTypeNameEnd))
                    {
                        throw new Exception("Parsing error: Invalid type name: " + memberTypeName);
                    }
                    
                    typeDefinition.Properties.Add(new ModelProperty
                    {
                        Name = memberName,
                        Attributes = attributes,
                        Type = ModelPropertyType.Object,
                        ClassName = memberTypeName,
                        InlineClassDefinition = hasInnerBlock ? ParseObjectType(innerBlockStart+1, innerBlockEnd) : null
                    });
                }

                SkipWhitespace(ref cursor);
                if (cursor == start)
                {
                    ++cursor;
                }
            }

            return typeDefinition;
        }

        ModelEnumDefinition ParseEnum(int blockStart, int blockEnd)
        {
            var values = input.Substring(blockStart, blockEnd - blockStart).Split(',').ToList();
            for (int i = 0; i < values.Count; ++i)
            {
                values[i] = values[i].Trim();

                if (values[i].StartsWith("//"))
                {
                    values.RemoveAt(i--);
                    continue;
                }
                
                if (!CheckIsValidMemberName(values[i]))
                {
                    throw new Exception("Parsing error: Invalid enum value name: " + values[i]);
                }
            }

            return new ModelEnumDefinition
            {
                Values = values
            };
        }

        void SkipWhitespace(ref int cursor)
        {
            while (cursor < input.Length && IsWhitespace(input[cursor]))
            {
                ++cursor;
            }
        }

        void SkipPast(ref int cursor, char[] chars)
        {
            while (cursor < input.Length && !Contains(chars, input[cursor]))
            {
                ++cursor;
            }
        }

        void BacktrackWhitespace(ref int cursor)
        {
            while (IsWhitespace(input[cursor]))
            {
                --cursor;
            }
        }
        
        void SkipUntilAny(ref int cursor, char[] chars)
        {
            while (!Contains(chars, input[cursor]))
            {
                ++cursor;
            }
        }
        
        void SkipUntil(ref int cursor, char c)
        {
            while (input[cursor] != c)
            {
                ++cursor;
            }
        }
        
        void SkipUntilWhitespace(ref int cursor)
        {
            while (!IsWhitespace(input[cursor]))
            {
                ++cursor;
            }
        }

        void SkipComments(ref int cursor)
        {
            while (TryRead(ref cursor, "//"))
            {
                SkipUntil(ref cursor, '\n');
                SkipWhitespace(ref cursor);
            }
        }

        bool TryRead(ref int cursor, string searchQuery)
        {
            if (input.Length - searchQuery.Length <= cursor)
            {
                return false;
            }
            for (int i = 0; i < searchQuery.Length; ++i)
            {
                if (input[cursor + i] != searchQuery[i])
                {
                    return false;
                }
            }

            cursor += searchQuery.Length;
            return true;
        }

        int GetBlockEnd(int blockStart)
        {
            int cur = blockStart+1;
            char c;
            while ((c = input[cur]) != '}')
            {
                if (c == '{')
                {
                    cur = GetBlockEnd(cur)+1;
                }
                else
                {
                    ++cur;
                }
            }

            return cur;
        }

        bool CheckIsValidMemberName(int start, int end)
        {
            if (!IsAlpha(input[start]))
            {
                return false;
            }
            
            for (int i = start+1; i < end; ++i)
            {
                if (!IsAlphaNumeric(input[i]))
                {
                    return false;
                }
            }

            return true;
        }
        
        bool CheckIsValidMemberName(string value)
        {
            if (!IsAlpha(value[0]))
            {
                return false;
            }
            
            for (int i = 1; i < value.Length; ++i)
            {
                if (!IsAlphaNumeric(value[i]))
                {
                    return false;
                }
            }

            return true;
        }

        bool IsAlpha(char c)
        {
            if(('A' <= c && c <= 'Z')
            || ('a' <= c && c <= 'z'))
            {
                return true;
            }

            if (c == '_')
            {
                return true;
            }

            return false;
        }

        bool IsAlphaNumeric(char c)
        {
            if (IsAlpha(c)
            || '0' <= c && c <= '9')
            {
                return true;
            }

            return false;
        }

        bool IsWhitespace(char c)
        {
            return Contains(WhitespaceChars, c);
        }

        bool Contains<T>(T[] arr, char c) where T : IEquatable<T>
        {
            for (int i = 0; i < arr.Length; ++i)
            {
                if (arr[i].Equals(c))
                {
                    return true;
                }
            }

            return false;
        }
    }
}