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
            string path = @"D:\Code\SceptrAPIs\New\Connector\Entities\Translators\";
            var fileGenerator = new FileGenerator(Assembly.LoadFrom(@"D:\Code\SceptrAPIs\New\Connector\Entities\Entities\bin\Debug\Tavisca.Connector.Hotels.Entities.dll"), "Tavisca.Connector.Hotels");
            var files = fileGenerator.TransformFiles();
            foreach (var fileName in files.Keys)
            {
                try
                {
                    var parts = fileName.Split('.');
                    Directory.CreateDirectory(path+ parts[parts.Length - 2]);
                    File.WriteAllText(path +parts[parts.Length-2]+@"\" + parts.Last()+"Translator.cs", files[fileName]);
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