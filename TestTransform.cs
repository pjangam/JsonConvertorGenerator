using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace JsonTranslator
{
    [TestClass]
    public class TestJson
    {
        [TestMethod]
        public void TestTransform()
        {
            string path = @"D:\Tavisca.USG.Cars.StateTranslators\";
            var fileGenerator = new FileGenerator(Assembly.LoadFrom(@"D:\Model.dll"), "Tavisca.USG.Cars.StateTranslators");
            var files = fileGenerator.TransformFiles();
            foreach (var fileName in files.Keys)
            {
                try
                {
                    var parts = fileName.Split('.');
                    Directory.CreateDirectory(path+ parts[parts.Length - 2]);
                    File.WriteAllText(path  + parts.Last()+"Translator.cs", files[fileName]);
                }
                catch (Exception ex)
                {
                    //File.WriteAllText(path + new Random(1000).Next().ToString() + "Translator.cs", files[fileName]);
                    Console.WriteLine(ex.ToString());
                }
            }
            Console.WriteLine("Done");

        }
    }
}