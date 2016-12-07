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
            string path = @"D:\Code\Platform\New\USG\Car\Tavisca.USG.Car\src\Tavisca.USG.Cars.Translators\";
            var fileGenerator = new FileGenerator(Assembly.LoadFrom(@"C:\Users\pjangam\Documents\visual studio 2015\Projects\ClassLibrary1\ClassLibrary1\bin\Debug\ClassLibrary1.dll"), " Tavisca.USG.Cars.Translators");
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