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

    partial class FileGenerator
    {
        public string Namespace { get; set; }
        public string ClassName { get; set; }
        public List<PropertyInfo> Properties { get; set; }
        private Assembly _assembly { get; set; }
        private List<Type> _types { get; set; }
        public FileGenerator(List<PropertyInfo> properties, string namespaceName, string className)
        {
            Properties = properties;
            ClassName = className;
            Namespace = namespaceName;

        }
        public FileGenerator(Assembly assembly, string namespaceName)
        {
            _assembly = assembly;
            _types = _assembly.GetTypes().OrderBy(t => t.Name).ToList();
            _types.RemoveAll(t => t.IsInterface || t.IsNestedPublic);
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
                var constructor = type.GetConstructor(Type.EmptyTypes);
                if (constructor == null)
                {
                    var fileGenerator = new ClassWithConstructorArguments(type.GetProperties().ToList(), Namespace, type.Name,type.GetConstructors());
                    return fileGenerator.TransformText();
                }
                else
                {
                    var fileGenerator = new FileGenerator(type.GetProperties().ToList(), Namespace, type.Name);
                    return fileGenerator.TransformText();
                }
            }


        }

        public Dictionary<string, string> TransformFiles()
        {
            var fileDictionary = new Dictionary<string, string>();
            var typesTobeConverted = _assembly.GetTypes();
            foreach (var type in typesTobeConverted)
            {
                if (type.IsAbstract)
                    continue;
                if (type.IsEnum)
                {
                    var enumtranslator=new EnumJsonTranslator(type.Namespace, type.Name, type);
                    fileDictionary.Add(type.Namespace + "." + type.Name,enumtranslator.TransformText());

                    continue;
                }

                var fileGenerator = new ClassWithConstructorArguments(type.GetProperties().ToList(), type.Namespace, type.Name,type.GetConstructors());
                fileDictionary.Add(type.Namespace + "." + type.Name, fileGenerator.TransformText());
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

                    if (propertyType == typeof(string) || propertyType == typeof(Guid) || propertyType.GetGenericTypeDefinition() == typeof(Nullable<>) || propertyType.IsPrimitive || (propertyType.GetGenericArguments().Any(t => t.IsValueType && t.IsPrimitive)))
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
                if (propertyTypeNamespace.StartsWith("System"))
                {
                    if (propertyType.IsPrimitive)
                    {
                        StringBuilder stringBuilder = new StringBuilder();
                        if (propertyType == typeof(float))
                        {
                            stringBuilder.AppendLine("{double value;");
                            stringBuilder.AppendLine("bool exists = v.TryAsFloat(out value);");
                            stringBuilder.AppendLine("if (exists == false)");
                            stringBuilder.AppendLine("throw new Exception(\"Exception occured while parsing" + propertyName + "\");");
                            stringBuilder.AppendLine("r." + propertyName + "=value;}");
                            return stringBuilder.ToString();
                        }
                        else if (propertyType == typeof(int))
                        {
                            stringBuilder.AppendLine("{int value;");
                            stringBuilder.AppendLine("bool exists = v.TryAsInt(out value);");
                            stringBuilder.AppendLine("if (exists == false)");
                            stringBuilder.AppendLine("throw new Exception(\"Exception occured while parsing" + propertyName + "\");");
                            stringBuilder.AppendLine("r." + propertyName + "=value;}");
                            return stringBuilder.ToString();
                        }
                        else if (propertyType == typeof(double))
                        {
                            stringBuilder.AppendLine("{double value;");
                            stringBuilder.AppendLine("bool exists = v.TryAsDouble(out value);");
                            stringBuilder.AppendLine("if (exists == false)");
                            stringBuilder.AppendLine("throw new Exception(\"Exception occured while parsing" + propertyName + "\");");
                            stringBuilder.AppendLine("r." + propertyName + "=value;}");
                            return stringBuilder.ToString();
                        }
                        else if (propertyType == typeof(decimal))
                        {
                            stringBuilder.AppendLine("{decimal value;");
                            stringBuilder.AppendLine("bool exists = v.TryAsDecimal(out value);");
                            stringBuilder.AppendLine("if (exists == false)");
                            stringBuilder.AppendLine("throw new Exception(\"Exception occured while parsing" + propertyName + "\");");
                            stringBuilder.AppendLine("r." + propertyName + "=value;}");
                            return stringBuilder.ToString();
                        }
                        else if (propertyType == typeof(bool))
                        {
                            stringBuilder.AppendLine("{bool value;");
                            stringBuilder.AppendLine("bool exists = v.TryAsBool(out value);");
                            stringBuilder.AppendLine("if (exists == false)");
                            stringBuilder.AppendLine("throw new Exception(\"Exception occured while parsing" + propertyName + "\");");
                            stringBuilder.AppendLine("r." + propertyName + "=value;}");
                            return stringBuilder.ToString();
                        }
                        else if (propertyType == typeof(Uri))
                        {
                            return "r." + propertyName + "=v.AsObject<" + propertyTypeName + ">()";
                        }

                    }
                    else if (propertyType == typeof(string) || propertyType == typeof(Guid))
                    {
                        return "r." + propertyName + "=v.AsString()";
                    }
                    else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() != typeof(Nullable<>))
                    {
                        return "r." + propertyName + ".AddRange(v.AsArray<" + propertyType.GetGenericArguments()[0].Name + ">())";
                    }
                    else if (propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        return GetPropertySetter(Nullable.GetUnderlyingType(propertyType), propertyName);
                    }
                    else if (propertyType.IsEnum)
                    {
                        return "r." + propertyName + "=v.ToString()";
                    }

                }
                else if (propertyType.IsClass || propertyType.IsValueType)
                {
                    return "r." + propertyName + "=v.AsObject<" + propertyTypeName + ">()";
                }

            }
            catch
            {

            }
            return string.Empty;
        }

        private string GetPropertySetter(PropertyInfo propertyInfo)
        {
            var propertyType = propertyInfo.PropertyType;
            var propertyName = propertyInfo.Name;
            return GetPropertySetter(propertyType, propertyName);

        }
    }
}
