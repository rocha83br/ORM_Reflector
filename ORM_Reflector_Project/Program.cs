using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq.Mapping;
using System.Text;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Data.Objects.DataClasses;

namespace ORMReflector
{
    class Program
    {
        static void Main(string[] args)
        {
            var reqArgs = 0;
            StringBuilder strGen = new StringBuilder();

            if ((args.Count() > reqArgs) && (args[0].Contains(":\\") || args[0].Equals("?")))
            {
                if (!args[0].Equals("?"))
                {
                    var assemblyName = args[0];
                    var typeFullName = args[1];
                    var behavior = args.Count() > 2 ? args[2] : string.Empty;
                    var fileName = args.Count() > 3 ? args[3] : string.Empty;
                    var className = args.Count() > 4 ? args[4] : string.Empty;
                    var reqMsg = args.Count() > 5 ? args[5] : string.Empty;
                    var lenMsg = args.Count() > 6 ? args[6] : string.Empty;
                    var typeNamespace = string.Empty;

                    var serialize = (!string.IsNullOrEmpty(behavior) && ((int.Parse(behavior) & 1) == 1));
                    var selfValidate = (!string.IsNullOrEmpty(behavior) && ((int.Parse(behavior) & 2) == 2));
                    var createWS = (!string.IsNullOrEmpty(behavior) && ((int.Parse(behavior) & 4) == 4));
                    var wcfReady = (!string.IsNullOrEmpty(behavior) && ((int.Parse(behavior) & 8) == 8));

                    var knownTypeTp = "[KnownType(typeof({0}))]";
                    var reqTypeTp = "\t\t[Required(ErrorMessage=\"{0}\")";

                    Console.Clear();

                    try
                    {
                        var objInstance = Activator.CreateInstanceFrom(assemblyName, typeFullName).Unwrap();
                        typeNamespace = string.Concat(objInstance.GetType().Namespace, ".");

                        var objProps = objInstance.GetType().GetProperties()
                                                  .Where(prp => !prp.PropertyType.Name.Equals("EntityReference`1")
                                                             && !prp.Name.Equals("EntityState")
                                                             && !prp.Name.Equals("EntityKey"));

                        var objMethods = objInstance.GetType().GetMethods()
                                                    .Where(mtd => !mtd.Name.StartsWith("get_")
                                                               && !mtd.Name.StartsWith("set_")
                                                               && !mtd.Name.StartsWith("add_")
                                                               && !mtd.Name.StartsWith("remove_")
                                                               && !mtd.Name.Equals("MethodInfo")
                                                               && !mtd.Name.Equals("ToString")
                                                               && !mtd.Name.Equals("Equals")
                                                               && !mtd.Name.Equals("GetHashCode")
                                                               && !mtd.Name.Equals("GetType")).ToArray();

                        if ((serialize || selfValidate || wcfReady) && (objProps.Count() == 0))
                            throw new Exception("No property found in the Class.");

                        if (createWS && (objMethods.Length == 0))
                            throw new Exception("No method found in the Class.");

                        if (string.IsNullOrEmpty(className))
                            className = objInstance.GetType().Name;

                        strGen.AppendLine("using System;");
                        strGen.AppendLine("using System.Runtime.Serialization;");
                        strGen.AppendLine("using System.ComponentModel.DataAnnotations;");
                        strGen.AppendLine("using System.Collections;");
                        strGen.AppendLine("using System.Collections.Generic;");

                        if (createWS)
                            strGen.AppendLine("using System.Web.Services;");

                        strGen.Append(Environment.NewLine);

                        if (serialize) strGen.AppendLine("[Serializable]");

                        if (createWS) strGen.AppendLine(@"[WebService(Namespace = ""http://tempuri.org/"")]");

                        if (wcfReady)
                        {
                            strGen.AppendLine("[DataContract(IsReference = true)]");
                            foreach (var prp in objProps.Where(obi => obi.PropertyType.IsClass))
                                strGen.AppendLine(string.Format(knownTypeTp, prp.PropertyType.Name));

                            strGen.Append(Environment.NewLine);
                        }

                        strGen.AppendLine(string.Concat("public class ", className, " {"));
                        strGen.AppendLine(string.Concat(Environment.NewLine, "\t#region Properties", Environment.NewLine));

                        int contAlpha = 97;
                        int contComplem = 1;
                        foreach (var prp in objProps)
                        {
                            if (contAlpha == 123)
                            {
                                contAlpha = 97;
                                contComplem = 1;
                            }

                            if (selfValidate)
                            {

                                var dataAttrib = prp.GetCustomAttributes(true)
                                                    .FirstOrDefault(cla => (cla is System.Data.Linq.Mapping.AssociationAttribute)
                                                                        || (cla is System.Data.Objects.DataClasses.EdmScalarPropertyAttribute));

                                if (dataAttrib != null)
                                {
                                    dynamic attribCfg = null;
                                    bool nullAttrib = false;
                                    if (dataAttrib != null)
                                    {
                                        try
                                        {
                                            attribCfg = (ColumnAttribute)dataAttrib;
                                            if (attribCfg.IsPrimaryKey) strGen.AppendLine("\t\t[Key]");
                                            nullAttrib = attribCfg.CanBeNull;
                                        }
                                        catch
                                        {
                                            attribCfg = (EdmScalarPropertyAttribute)dataAttrib;
                                            if (attribCfg.EntityKeyProperty) strGen.AppendLine("\t\t[Key]");
                                            nullAttrib = attribCfg.IsNullable;
                                        }

                                        if (!nullAttrib)
                                        {
                                            if (string.IsNullOrEmpty(reqMsg))
                                                strGen.AppendLine("\t\t[Required]");
                                            else
                                                strGen.AppendLine(string.Format(reqTypeTp, string.Format(reqMsg, prp.PropertyType.Name)));
                                        }
                                    }
                                }
                            }

                            if (serialize)
                                strGen.AppendLine(string.Format("\t\t[JsonProperty(PropertyName = \"{0}\")]",
                                                  ((char)contAlpha++).ToString(), contComplem++.ToString()));

                            if (wcfReady) strGen.AppendLine("\t\t[DataMember]");

                            if (!prp.PropertyType.Name.Contains("`1"))
                                strGen.AppendLine(string.Concat("\t\tpublic ", prp.PropertyType.Name, " ",
                                                                               prp.Name, " { get; set; }", Environment.NewLine));
                            else
                            {
                                var childTypeName = prp.PropertyType.FullName.Substring(prp.PropertyType.FullName.IndexOf("[["));
                                childTypeName = childTypeName.Split(',')[0].Replace("[[", string.Empty);
                                childTypeName = childTypeName.Replace(string.Concat(prp.ReflectedType.Namespace, "."), string.Empty)
                                                             .Replace("`1", string.Empty).Replace("EntitySet", "List")
                                                                                         .Replace("EntityCollection", "List");

                                strGen.AppendLine(
                                    string.Concat("\t\tpublic ", prp.PropertyType.Name.Replace("`1", string.Empty)
                                                                    .Replace("EntitySet", "List")
                                                                    .Replace("EntityCollection", "List"), "<", childTypeName,
                                                                    "> ", prp.Name, " { get; set; }", Environment.NewLine));
                            }
                        }

                        foreach (var mtd in objMethods)
                        {
                            if (createWS)
                            {
                                    
                            }
                        }

                        strGen.AppendLine(string.Concat(Environment.NewLine, "\t#endregion", Environment.NewLine, Environment.NewLine, "}"));
                        strGen.AppendLine(string.Concat(Environment.NewLine, "Copy it to your Solution ;-)"));

                        Console.Write(strGen.ToString());

                        if (!string.IsNullOrEmpty(fileName))
                        {
                            File.WriteAllText(fileName, strGen.ToString());
                            Process.Start("Notepad.exe", fileName);
                        }

                        Console.Read();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.Read();
                    }
                }
                else
                {
                    Console.WriteLine("Use : [Library Full Path], [Full Ojbect Name], [Behavior], [Class Name], [Destination File], [Required Valid. Msg], [Length Valid. Msg]");
                    Console.Write(Environment.NewLine);
                    Console.WriteLine("Behaviors : 1 - Serializable");
                    Console.WriteLine("            2 - Self-Validatable");
                    Console.WriteLine("            3 - Create WebService");
                    Console.WriteLine("            4 - WCF Ready");
                    Console.Read();
                }
            }
            else
                Console.Write("Invalid argument.");
        }
    }
}
