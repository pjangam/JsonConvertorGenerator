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
        public List<string> ConstructorParameters { get; set; }

        public ClassWithConstructorArguments(List<PropertyInfo> properties, string namespaceName, string className, ConstructorInfo[] constructors)
        {
            Properties = properties;
            ClassName = className;
            Namespace = namespaceName;
            ConstructorParameters = constructors.Last().GetParameters().Select(p=>p.Name).ToList();
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
        public string ToCamelCase(string the_string)
        {
            return the_string.Substring(0, 1).ToLower() +
                the_string.Substring(1);
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
                        stringBuilder.AppendLine("DateTime " + propertyName + ";");
                        stringBuilder.AppendLine("DateTime.TryParse(json.ReadAsString(\"" + propertyName + "\"), out " + propertyName + ");");
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
                        stringBuilder.AppendLine("Guid.TryParse(json.ReadAsString(\"" + GetJsonPropertyName(propertyName) + "\"), out " + propertyName + ");");
                        return stringBuilder.ToString();
                    }
                    else if (propertyType == typeof(int))
                    {
                        stringBuilder.AppendLine("int " + propertyName + ";");
                        stringBuilder.AppendLine("int.TryParse(json.ReadAsString(\"" + GetJsonPropertyName(propertyName) + "\"), out " + propertyName + ");");
                        return stringBuilder.ToString();
                    }
                    else if (propertyType == typeof(decimal))
                    {
                        stringBuilder.AppendLine("decimal " + propertyName + ";");
                        stringBuilder.AppendLine("decimal.TryParse(json.ReadAsString(\"" + GetJsonPropertyName(propertyName) + "\"), out " + propertyName + ");");
                        return stringBuilder.ToString();
                    }
                    else if (propertyType == typeof(float))
                    {
                        stringBuilder.AppendLine("float " + propertyName + ";");
                        stringBuilder.AppendLine("float.TryParse(json.ReadAsString(\"" + GetJsonPropertyName(propertyName) + "\"), out " + propertyName + ");");
                        return stringBuilder.ToString();
                    }
                    else if (propertyType == typeof(double))
                    {
                        stringBuilder.AppendLine("double " + propertyName + ";");
                        stringBuilder.AppendLine("double.TryParse(json.ReadAsString(\"" + GetJsonPropertyName(propertyName) + "\"), out " + propertyName + ");");
                        return stringBuilder.ToString();
                    }
                    else if (propertyType == typeof(bool))
                    {
                        stringBuilder.AppendLine("bool " + propertyName + ";");
                        stringBuilder.AppendLine("bool.TryParse(json.ReadAsString(\"" + GetJsonPropertyName(propertyName) + "\"), out " + propertyName + ");");
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
            { "description","desc" },
            {"number","num" }
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




        private string GetPropertySetter(PropertyInfo propertyInfo)
        {
            var propertyType = propertyInfo.PropertyType;
            var propertyName = ToCamelCase(propertyInfo.Name);
            return GetPropertySetter(propertyType, propertyName);

        }
    }
}
