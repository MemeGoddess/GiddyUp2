using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GiddyUpCore.MCP
{
    internal static class MCPHelper
    {
        //internal static JObject ObjToMCP(IList<Type> types)
        //{
        //    var objectSchema = new JObject
        //    {
        //        ["type"] = "object",
        //        ["properties"] = types.ToDictionary(x => x.Name)
        //    };
        //}

        internal static JObject ObjToMCP(Type type)
        {
            return BuildSchema(type, new HashSet<Type>());
        }

        private static JObject BuildSchema(Type rawType, HashSet<Type> seenTypes)
        {
            var type = Nullable.GetUnderlyingType(rawType) ?? rawType;

            if (type.IsEnum)
            {
                return new JObject
                {
                    ["type"] = "string",
                    ["enum"] = new JArray(Enum.GetNames(type))
                };
            }

            if (type == typeof(string)
                || type == typeof(char)
                || type == typeof(Guid)
                || type == typeof(DateTime)
                || type == typeof(DateTimeOffset)
                || type == typeof(TimeSpan)
                || type == typeof(Uri))
            {
                return new JObject
                {
                    ["type"] = "string"
                };
            }

            if (type == typeof(bool))
            {
                return new JObject
                {
                    ["type"] = "boolean"
                };
            }

            if (type == typeof(byte)
                || type == typeof(sbyte)
                || type == typeof(short)
                || type == typeof(ushort)
                || type == typeof(int)
                || type == typeof(uint)
                || type == typeof(long)
                || type == typeof(ulong))
            {
                return new JObject
                {
                    ["type"] = "integer"
                };
            }

            if (type == typeof(float)
                || type == typeof(double)
                || type == typeof(decimal))
            {
                return new JObject
                {
                    ["type"] = "number"
                };
            }

            if (TryGetEnumerableElementType(type, out var elementType))
            {
                return new JObject
                {
                    ["type"] = "array",
                    ["items"] = BuildSchema(elementType, seenTypes)
                };
            }

            if (!seenTypes.Add(type))
            {
                return new JObject
                {
                    ["type"] = "object"
                };
            }

            var properties = new JObject();
            var required = new JArray();
            var members = type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
                .Cast<MemberInfo>()
                .Concat(type.GetFields(BindingFlags.Instance | BindingFlags.Public))
                .ToList();

            foreach (var member in members)
            {
                Type memberType;
                switch (member)
                {
                    case PropertyInfo propertyInfo:
                        memberType = propertyInfo.PropertyType;
                        break;
                    case FieldInfo fieldInfo:
                        memberType = fieldInfo.FieldType;
                        break;
                    default:
                        continue;
                }

                //var memberSchema = BuildSchema(memberType, seenTypes);
                //var description = member.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description;
                //if (!string.IsNullOrWhiteSpace(description))
                //    memberSchema["description"] = description;

                //properties[member.Name] = memberSchema;

                //if (member.GetCustomAttribute<RequiredAttribute>() != null
                //    || IsRequiredMember(memberType))
                //{
                //    required.Add(member.Name);
                //}
            }

            seenTypes.Remove(type);

            var objectSchema = new JObject
            {
                ["type"] = "object",
                ["properties"] = properties
            };

            if (required.Count > 0)
                objectSchema["required"] = required;

            return objectSchema;
        }

        private static bool IsRequiredMember(Type memberType)
        {
            return memberType.IsValueType && Nullable.GetUnderlyingType(memberType) == null;
        }

        private static bool TryGetEnumerableElementType(Type type, out Type elementType)
        {
            if (type.IsArray)
            {
                elementType = type.GetElementType() ?? typeof(object);
                return true;
            }

            if (type == typeof(string))
            {
                elementType = typeof(string);
                return false;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }

            var enumerableInterface = type.GetInterfaces()
                .FirstOrDefault(interfaceType =>
                    interfaceType.IsGenericType
                    && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (enumerableInterface != null)
            {
                elementType = enumerableInterface.GetGenericArguments()[0];
                return true;
            }

            elementType = typeof(object);
            return false;
        }
    }
}
