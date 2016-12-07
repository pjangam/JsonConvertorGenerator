using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace JsonTranslator
{
    partial class EnumJsonTranslator
    {
        public string EnumNamespace { get; set; }
        public string EnumName { get; set; }
        public List<string> EnumValues { get; set; }
        public EnumJsonTranslator(string enumNamespace, string enumName, Type enumType)
        {
            EnumNamespace = enumNamespace;
            EnumName = enumName;
            EnumValues = Enum.GetNames(enumType).ToList();
        }

        public string ToCamelCase(string the_string)
        {
            return the_string.Substring(0, 1).ToLower() +
                the_string.Substring(1);
        }

        public string GetCommaSeperated(List<string> ls)
        {
            return string.Join(",",ls);
        }
    }
}
