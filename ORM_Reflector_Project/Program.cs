using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel.DataAnnotations;

namespace ORM_Reflector
{
    class Program
    {
        static void Main(string[] args)
        {
            var reqArgs = 2;
            StringBuilder strGen = new StringBuilder();

            if (args.Count().Equals(reqArgs) && args[0].Contains(":\\"))
            {
                var assemblyName = args[0];
                var typeFullName = args[1];
                var className = args.Count() == 3 ? args[2] : string.Empty;
                var fileName = args.Count() == 4 ? args[3] : string.Empty;
                var typeNamespace = string.Empty;
                
                if (!args[0].Equals("?"))
                {
                    Console.Clear();

                    try
                    {
                        var objInstance = Activator.CreateInstanceFrom(assemblyName, typeFullName).Unwrap();
                        typeNamespace = string.Concat(objInstance.GetType().Namespace, ".");

                        if (!string.IsNullOrEmpty(className))
                            className = objInstance.GetType().Name;

                        strGen.AppendLine("using System;");
                        strGen.AppendLine("using System.ComponentModel.DataAnnotations;");
                        strGen.AppendLine("using System.Collections;");
                        strGen.AppendLine("using System.Collections.Generic;");
                        strGen.AppendLine("using System.Text;");
                        strGen.AppendLine("using System.Linq;");

                        strGen.AppendLine(string.Concat(Environment.NewLine, "[Seriarilizable]"));
                        strGen.AppendLine(string.Concat("public class ", className, " {"));
                        strGen.AppendLine(string.Concat(Environment.NewLine, "\t#region Properties", Environment.NewLine));

                        foreach (var prp in objInstance.GetType().GetProperties())
                        {
                            if (!prp.PropertyType.Name.Contains("`1"))
                                strGen.AppendLine(string.Concat("\t\tpublic ", prp.PropertyType.Name, " ", 
                                                                               prp.Name, " { get; set; }", Environment.NewLine));
                            else
                            {
                                var childTypeName = prp.PropertyType.FullName.Substring(prp.PropertyType.FullName.IndexOf("[["));
                                childTypeName = childTypeName.Split(',')[0].Replace("[[", string.Empty);
                                childTypeName = childTypeName.Replace(string.Concat(prp.ReflectedType.Namespace, "."), string.Empty)
                                                             .Replace("`1", string.Empty).Replace("EntitySet", "List");

                                strGen.AppendLine(
                                    string.Concat("\t\tpublic ", prp.PropertyType.Name.Replace("`1", string.Empty)
                                                                    .Replace("EntitySet", "List"), "<", childTypeName,
                                                                    "> ", prp.Name, " { get; set; }", Environment.NewLine));
                            }
                        }

                        strGen.AppendLine(string.Concat(Environment.NewLine, "\t#endregion", Environment.NewLine, Environment.NewLine, "}"));
                        strGen.AppendLine(string.Concat(Environment.NewLine, "Copy it to your Solution ;-)"));

                        Console.Write(strGen.ToString());

                        if (!string.IsNullOrEmpty(fileName))
                            File.WriteAllText(fileName, strGen.ToString());
                        
                        Console.Read();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.Read();
                    }                   
                }
                else
                    Console.Write("Use : [Library Full Path], [Full Ojbect Name], [Class Name], [Destination File]");
            }
            else
                Console.Write("Argumento inválido.");
        }
    }
}
