using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace JsonTranslator
{
    partial class EnumJsonTranslator
    {
        private Dictionary<string, string> _propertyMapper = new Dictionary<string, string> {
            { "description","desc" },
            {"number","num" }
        };

        public string EnumNamespace { get; set; }
        public string EnumName { get; set; }
        public List<string> EnumValues { get; set; }
        public EnumJsonTranslator(string enumNamespace, string enumName, Type enumType)
        {
            EnumNamespace = enumNamespace;
            EnumName = enumName;
            EnumValues = Enum.GetNames(enumType).ToList();
        }

        private string GetJsonPropertyName(string propertyName)
        {
            string result;
            if (!_propertyMapper.TryGetValue(propertyName, out result))
            {
                result = propertyName;
            }
            return result;
        }
      
        public string GetCommaSeperated(List<string> y)
        {
            return string.Join(", ", EnumValues.Select(x => x).ToArray());
        }
    }
}