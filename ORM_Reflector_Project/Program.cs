using System;
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
                var typeNamespace = string.Empty;
                
                if (!args[0].Equals("?"))
                {
                    Console.Clear();

                    try
                    {
                        var objInstance = Activator.CreateInstanceFrom(assemblyName, typeFullName).Unwrap();
                        typeNamespace = string.Concat(objInstance.GetType().Namespace, ".");

                        strGen.AppendLine("using System;");
                        strGen.AppendLine("using System.Collections;");
                        strGen.AppendLine("using System.Collections.Generic;");
                        strGen.AppendLine("using System.Linq;");
                        strGen.AppendLine("using System.Text;");
                        strGen.AppendLine("using System.ComponentModel.DataAnnotations;");

                        strGen.AppendLine(string.Concat(Environment.NewLine, "[Seriarilizable]"));
                        strGen.AppendLine(string.Concat("public class ", objInstance.GetType().Name, " {"));
                        strGen.AppendLine(string.Concat(Environment.NewLine, "\t#region Properties", Environment.NewLine));

                        foreach (var prp in objInstance.GetType().GetProperties())
                        {
                            if (!prp.PropertyType.Name.Contains("`1"))
                                strGen.AppendLine(
                                    string.Concat("\t\tpublic ", prp.PropertyType.Name, " ", prp.Name, " { get; set; }"));
                            else
                            {
                                var childTypeName = prp.PropertyType.FullName.Replace("System.Data.Linq.EntitySet`1[[", string.Empty).Split(',')[0];
                                strGen.AppendLine(
                                    string.Concat("\t\tpublic List<",
                                        childTypeName.Replace(typeNamespace, string.Empty), "> ", prp.Name, " { get; set; }"));
                            }
                        }

                        strGen.AppendLine(string.Concat(Environment.NewLine, "\t#endregion", Environment.NewLine, Environment.NewLine, "}"));
                        strGen.AppendLine(string.Concat(Environment.NewLine, "Copy it to your Solution ;-)"));

                        Console.Write(strGen.ToString());
                        Console.Read();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.Read();
                    }                   
                }
                else
                    Console.Write("Forma de uso : [Nome da biblioteca], [Namespace completo do objeto], [Arquivo destino]");
            }
            else
                Console.Write("Argumento inválido.");
        }
    }
}
