using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JsonTranslator
{

    partial class ClassWithConstructorArguments
    {
        public string Namespace { get; set; }
        public string ClassName { get; set; }
        public List<PropertyInfo> Properties { get; set; }
        private Assembly _assembly { get; set; }
        private List<Type> _types { get; set; }
        public List<List<string>> ConstructorsParameters { get; set; }

        public List<string> fieldsCreated { get; set; }

        public ClassWithConstructorArguments(List<PropertyInfo> properties, string namespaceName, string className, ConstructorInfo[] constructors)
        {
            Properties = properties;
            ClassName = className;
            Namespace = namespaceName;
            ConstructorsParameters = constructors.Select(x => x.GetParameters().Select(p => p.Name).ToList()).ToList();
            fieldsCreated = new List<string>();
        }
        public ClassWithConstructorArguments(Assembly assembly, string namespaceName)
        {
            _assembly = assembly;
            _types = _assembly.GetTypes().OrderBy(t => t.Name).ToList();
            _types.RemoveAll(t => t.IsSealed || t.IsInterface || t.IsNestedPublic || !t.IsSerializable);
            Namespace = namespaceName;
        }
        public string GenerateClassFileFromAssembly(string className)
        {
            var type = _types.SingleOrDefault(t => t.Name == className);
            Properties = type.GetProperties().ToList();
            ClassName = type.Name;
            if (type.IsEnum)
            {
                var enumFileGenerator = new EnumJsonTranslator(Namespace, ClassName, type);
                return enumFileGenerator.TransformText();
            }
            else
            {
                var fileGenerator = new FileGenerator(type.GetProperties().ToList(), Namespace, type.Name);
                return fileGenerator.TransformText();
            }
        }

        public Dictionary<string, string> TransformFiles()
        {
            var fileDictionary = new Dictionary<string, string>();
            var typesTobeConverted = _assembly.GetTypes();
            foreach (var type in typesTobeConverted)
            {
                var fileGenerator = new FileGenerator(type.GetProperties().ToList(), Namespace, type.Name);
                fileDictionary.Add(type.Name, fileGenerator.TransformText());
            }
            return fileDictionary;
        }


        public bool IsSerializerRequired(PropertyInfo propertyInfo)
        {
            try
            {
                var propertyTypeNamespace = propertyInfo.PropertyType.Namespace;
                var propertyType = propertyInfo.PropertyType;
                if (propertyTypeNamespace.StartsWith("System"))
                {
                    if (propertyType == typeof(Uri))
                        return true;

                    if (propertyType == typeof(string) || propertyType == typeof(Guid) || propertyType.GetGenericTypeDefinition() == typeof(Nullable<>) || propertyType.IsPrimitive)
                    {
                        return false;
                    }


                }
            }
            catch
            {
                return false;
            }

            return true;
        }
        private string GetPropertySetter(Type propertyType, string propertyName)
        {
            try
            {
                var propertyTypeName = propertyType.Name;
                var propertyTypeNamespace = propertyType.Namespace;
                StringBuilder stringBuilder = new StringBuilder();
                if (propertyTypeNamespace.StartsWith("System"))
                {
                    if (propertyType == typeof(DateTime))
                    {
                        stringBuilder.AppendLine("var " + propertyName + "=json.ReadAsDateTime(\"" + GetJsonPropertyName(propertyName) + "\",reader.Path);");
                        return stringBuilder.ToString();
                    }
                    else if (propertyType == typeof(string))
                    {
                        stringBuilder.AppendLine("var " + propertyName + "=json.ReadAsString(\"" + GetJsonPropertyName(propertyName) + "\");");
                        return stringBuilder.ToString();
                    }
                    else if (propertyType == typeof(Guid))
                    {
                        stringBuilder.AppendLine("Guid " + propertyName + ";");
                        stringBuilder.AppendLine("\t\t\tGuid.TryParse(json.ReadAsString(\"" + GetJsonPropertyName(propertyName) + "\"), out " + propertyName + ");");
                        return stringBuilder.ToString();
                    }
                    else if (propertyType == typeof(int))
                    {
                        stringBuilder.AppendLine("int " + propertyName + ";");
                        stringBuilder.AppendLine("\t\t\tint.TryParse(json.ReadAsString(\"" + GetJsonPropertyName(propertyName) + "\"), out " + propertyName + ");");

                        return stringBuilder.ToString();
                    }
                    else if (propertyType == typeof(decimal))
                    {
                        stringBuilder.AppendLine("decimal " + propertyName + ";");
                        stringBuilder.AppendLine("\t\t\tdecimal.TryParse(json.ReadAsString(\"" + GetJsonPropertyName(propertyName) + "\"), out " + propertyName + ");");

                        return stringBuilder.ToString();
                    }
                    else if (propertyType == typeof(float))
                    {
                        stringBuilder.AppendLine("float " + propertyName + ";");
                        stringBuilder.AppendLine("\t\t\tfloat.TryParse(json.ReadAsString(\"" + GetJsonPropertyName(propertyName) + "\"), out " + propertyName + ");");

                        return stringBuilder.ToString();
                    }
                    else if (propertyType == typeof(bool))
                    {
                        stringBuilder.AppendLine("bool " + propertyName + ";");
                        stringBuilder.AppendLine("\t\t\tbool.TryParse(json.ReadAsString(\"" + GetJsonPropertyName(propertyName) + "\"), out " + propertyName + ");");

                        return stringBuilder.ToString();
                    }
                    else if (propertyType == typeof(Nullable<bool>))
                    {
                        stringBuilder.AppendLine("bool? " + propertyName + "=");
                        stringBuilder.Append("json.ReadAsNullableBool(\"" + GetJsonPropertyName(propertyName) + "\",reader.Path);");
                        return stringBuilder.ToString();
                    }
                    else if (propertyType == typeof(Nullable<int>))
                    {
                        stringBuilder.AppendLine("int? " + propertyName + "=");
                        stringBuilder.Append("json.ReadAsNullableInt(\"" + GetJsonPropertyName(propertyName) + "\",reader.Path);");
                        return stringBuilder.ToString();
                    }
                    else if (propertyType == typeof(Nullable<decimal>))
                    {
                        stringBuilder.AppendLine("decimal? " + propertyName + "=");
                        stringBuilder.Append("json.ReadAsNullableDecimal(\"" + GetJsonPropertyName(propertyName) + "\",reader.Path);");
                        return stringBuilder.ToString();
                    }
                    else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() != typeof(Nullable<>))
                    {
                        return "ReadAsArray<" + propertyType.GetGenericArguments()[0].Name + "> (\"" + propertyName + "\"";

                    }
                    else if (propertyType.IsEnum)
                    {
                        return "ReadAsEnum<" + propertyTypeName + ">(\"" + GetJsonPropertyName(propertyName) + "\", default(" + propertyTypeName + ")";
                    }
                    else if (propertyType == typeof(Uri))
                    {
                        return "ReadAsObject<" + GetJsonPropertyName(propertyName) + ">(\"" + GetJsonPropertyName(propertyName) + "\"";
                    }


                }
                else if (propertyType.IsClass || propertyType.IsValueType)
                {
                    return "ReadAsObject<" + propertyTypeName + ">(\"" + GetJsonPropertyName(propertyName) + "\"";
                }

            }
            catch
            {

            }
            return string.Empty;
        }

        private Dictionary<string, string> _propertyMapper = new Dictionary<string, string> {
            //{ "description","desc" },
            //{"number","num" }
        };

        private string GetJsonPropertyName(string propertyName)
        {
            string result;
            if (!_propertyMapper.TryGetValue(propertyName, out result))
            {
                result = propertyName;
            }
            return result;
        }


        private string GetJsonPropertyName(PropertyInfo prop)
        {
            if (prop.CustomAttributes != null)
            {
                var attributes = prop.CustomAttributes.ToList();
                var jsonAttr = attributes.FirstOrDefault(a => a.AttributeType.Name == "JsonPropertyAttribute");
                if (jsonAttr != null)
                {
                    return jsonAttr.ConstructorArguments.FirstOrDefault().Value.ToString();
                }
                else
                {
                    return prop.Name;
                }
            }
            else
            {
                return prop.Name;
            }
            return "todo";
        }

        private string GetPropertySetter(PropertyInfo propertyInfo)
        {
            var propertyType = propertyInfo.PropertyType;
            var propertyName = (propertyInfo.Name);
            return GetPropertySetter(propertyType, propertyName);

        }
    }
}
