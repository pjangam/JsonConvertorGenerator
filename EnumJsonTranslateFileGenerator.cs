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
    }
}
