using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace NRedisPlus.Generators
{
    [Generator]
    public class RedisHashableGenerator : ISourceGenerator
    { 
        private static bool IsPrimitive(PropertyDeclarationSyntax p)
        {
            return IsPrimitive(p.Type.ToString());
        }

        private static bool IsPrimitive(string type)
        {   
            switch( type)
            {
                case "bool":
                case "Boolean":
                case "sbyte":
                case "SByte":
                case "Byte":
                case "byte":
                case "Int16":
                case "short":
                case "Int32":
                case "int":
                case "Int64":
                case "long":
                case "UInt16":
                case "ushort":
                case "UInt32":
                case "uint":
                case "UInt64":
                case "ulong":
                case "Single":
                case "float":
                case "Double":
                case "double":
                case "Char":
                case "char":
                case "String":
                case "string":
                case "Object":
                case "object":
                    return true;
            }
            
            return false;
        }
        
        private static string Generate(ClassDeclarationSyntax c)
        {
            var seralizeSb = new StringBuilder();
            var hydrateSb = new StringBuilder();            
            int indent = 8;            
            foreach(var member in c.Members)
            {
                if(member is PropertyDeclarationSyntax p)
                {
                    var isList = p.AttributeLists.Any(x => x.Attributes.Any(a => a.Name.ToString() == "ListType"));                    
                    if (isList)
                    {
                        var genericType = ((GenericNameSyntax)p.Type).TypeArgumentList.Arguments[0].ToString();
                        
                        seralizeSb.Append(' ', indent);
                        seralizeSb.Append($"if({p.Identifier} != null){Environment.NewLine}");
                        seralizeSb.Append(' ', indent);
                        seralizeSb.Append($"{{{Environment.NewLine}");
                        seralizeSb.Append(' ', indent+4);
                        seralizeSb.Append($"for(var i = 0; i < {p.Identifier}.Count; i++){Environment.NewLine}");
                        seralizeSb.Append(' ', indent+4);
                        seralizeSb.Append($"{{{Environment.NewLine}");
                        hydrateSb.Append(' ', indent);
                        hydrateSb.Append($"this.{p.Identifier} = new List<{genericType}>();{Environment.NewLine}");
                        hydrateSb.Append(' ', indent);
                        hydrateSb.Append($"var k = 0;{Environment.NewLine}");
                        if (IsPrimitive(genericType))
                        {
                            
                            seralizeSb.Append(' ', indent+8);
                            seralizeSb.Append($"dict.Add($\"{ p.Identifier}[{{i}}]\", {p.Identifier}[i]);{Environment.NewLine}");

                            hydrateSb.Append(' ', indent);
                            hydrateSb.Append($"while(dict.ContainsKey($\"{p.Identifier}[{{k}}]\"){Environment.NewLine})");
                            hydrateSb.Append(' ', indent);
                            hydrateSb.Append($"{{{Environment.NewLine}");
                            if(genericType == "string" || genericType == "String")
                            {                                
                                hydrateSb.Append(' ', indent + 8);
                                hydrateSb.Append($"this.{p.Identifier}.Add(dict[$\"{p.Identifier}[{{k}}]\"]);{Environment.NewLine}");
                            }
                            else
                            {
                                hydrateSb.Append(' ', indent + 4);
                                hydrateSb.Append($"this.{p.Identifier}.Add({genericType}.Parse(dict[$\"{p.Identifier}[{{k}}]\"]));{Environment.NewLine}");
                            }
                            hydrateSb.Append(' ', indent + 4);
                            hydrateSb.Append($"k++;{Environment.NewLine}");
                            hydrateSb.Append(' ', indent);
                            hydrateSb.Append($"}}{Environment.NewLine}");
                        }
                        else
                        {
                            seralizeSb.Append(' ', indent + 8);
                            seralizeSb.Append($"var subHash = {p.Identifier}[i].BuildHashSet();");
                            seralizeSb.Append(' ', indent + 8);
                            seralizeSb.Append("foreach(var pair in subHash");
                            seralizeSb.Append(' ', indent + 8);
                            seralizeSb.Append($"{{{Environment.NewLine}");
                            seralizeSb.Append(' ', indent + 12);
                            seralizeSb.Append($"dict.Add($\"{p.Identifier}[{{i}}].{{pair.Key}}\",{{pair.Value}});");
                            seralizeSb.Append(' ', indent + 8);
                            seralizeSb.Append($"}}{Environment.NewLine}");
                            
                            hydrateSb.Append(' ', indent);
                            hydrateSb.Append($"var subDict = dict.Where(x=>x.Key.StartsWith($\"{p.Identifier}[{{k}}]\")).ToDictionary(x=>x.Key.Substring({p.Identifier.ToString().Length + 1} + k.ToString().Length, x=>x.Value);");
                            hydrateSb.Append(' ', indent);
                            hydrateSb.Append("while(subDict.Any())");
                            hydrateSb.Append(' ', indent);
                            hydrateSb.Append($"{{{Environment.NewLine}");
                            hydrateSb.Append(' ', indent+4);
                            hydrateSb.Append($"this.{p.Identifier}.Add(new {genericType}().Hydrate(subDict));");
                            hydrateSb.Append(' ', indent + 4);
                            hydrateSb.Append($"k++;");
                            hydrateSb.Append(' ', indent + 4);
                            hydrateSb.Append($"var subDict = dict.Where(x=>x.Key.StartsWith($\"{p.Identifier}[{{k}}]\")).ToDictionary(x=>x.Key.Substring({p.Identifier.ToString().Length + 1} + k.ToString().Length, x=>x.Value);");
                            hydrateSb.Append(' ', indent);
                            hydrateSb.Append($"}}{Environment.NewLine}");
                        }
                        seralizeSb.Append(' ', indent+4);
                        seralizeSb.Append($"}}{Environment.NewLine}");
                        seralizeSb.Append(' ', indent);
                        seralizeSb.Append($"}}{Environment.NewLine}");

                    }
                    else if (IsPrimitive(p))
                    {
                        seralizeSb.Append(' ', indent);
                        seralizeSb.Append($"if({p.Identifier} != default({p.Type})){Environment.NewLine}");
                        hydrateSb.Append(' ', indent);
                        hydrateSb.Append($"if(dict.ContainsKey(\"{p.Identifier}\")){Environment.NewLine}");
                        if(p.Type.ToString() == "string" || p.Type.ToString() == "String")
                        {
                            seralizeSb.Append(' ', indent+4);
                            seralizeSb.Append($"dict.Add(\"{p.Identifier}\",this.{p.Identifier});{Environment.NewLine}");

                            hydrateSb.Append(' ', indent + 4);
                            hydrateSb.Append($"this.{p.Identifier} = dict[\"{p.Identifier}\"];{Environment.NewLine}");
                        }
                        else
                        {
                            seralizeSb.Append(' ', indent+4);
                            seralizeSb.Append($"dict.Add(\"{p.Identifier}\",this.{p.Identifier}.ToString());{Environment.NewLine}");
                            hydrateSb.Append(' ', indent + 4);
                            hydrateSb.Append($"this.{p.Identifier} = {p.Type.ToString()}.Parse(dict[\"{p.Identifier}\"]);{Environment.NewLine}");
                        }
                        
                    }
                    else
                    {
                        seralizeSb.Append(' ', indent);
                        seralizeSb.Append($"if(this.{p.Identifier} != null){Environment.NewLine}");
                        seralizeSb.Append(' ', indent);
                        seralizeSb.Append($"{{{Environment.NewLine}");
                        seralizeSb.Append(' ', indent + 4);
                        seralizeSb.Append($"foreach(var pair in {p.Identifier}.BuildHashSet()){Environment.NewLine}");
                        seralizeSb.Append(' ', indent + 4);
                        seralizeSb.Append($"{{{Environment.NewLine}");
                        seralizeSb.Append(' ', indent + 8);
                        seralizeSb.Append($"dict.Add($\"{p.Identifier}.{{pair.Key}}\", pair.Value);{Environment.NewLine}");
                        seralizeSb.Append(' ', indent + 4);
                        seralizeSb.Append($"}}{Environment.NewLine}");
                        seralizeSb.Append(' ', indent);
                        seralizeSb.Append($"}}{Environment.NewLine}");

                        hydrateSb.Append(' ', indent);
                        hydrateSb.Append($"var {p.Identifier}Fields = dict.Where(x=>x.Key.StartsWith(\"{p.Identifier}.\")).ToDictionary(x=>x.Key.Substring({p.Identifier.ToString().Length + 1}), x=> x.Value);{Environment.NewLine}");
                        hydrateSb.Append(' ', indent);
                        hydrateSb.Append($"if({p.Identifier}Fields.Any()){Environment.NewLine}");
                        hydrateSb.Append(' ', indent);
                        hydrateSb.Append($"{{{Environment.NewLine}");
                        hydrateSb.Append(' ', indent+4);
                        hydrateSb.Append($"this.{p.Identifier} = new {p.Type}();{Environment.NewLine}");
                        hydrateSb.Append(' ', indent + 4);
                        hydrateSb.Append($"this.{p.Identifier}.Hydrate({p.Identifier}Fields);{Environment.NewLine}");
                        hydrateSb.Append(' ', indent);
                        hydrateSb.Append($"}}{Environment.NewLine}");
                    }                    
                }
            }
            try
            {
                var className = c.Identifier.ToString();
                var ret = $"using System.Collections.Generic;{Environment.NewLine}" +
                    $"using System.Linq;{Environment.NewLine}" +
                    $"using NRediPlus;{Environment.NewLine}" +
                    $"namespace TestClient{Environment.NewLine}" +
                    $"{{{Environment.NewLine}" +
                    $"public partial class {className} : IRedisHydrateable{Environment.NewLine}" +
                    $"{{{Environment.NewLine}" +
                    $"    public IDictionary<string,string> BuildHashSet(){Environment.NewLine}" +
                    $"    {{{Environment.NewLine}" +
                    $"        var dict = new Dictionary<string,string>();{Environment.NewLine}" +
                    $"        {Environment.NewLine}" +
                    $"{seralizeSb}{Environment.NewLine}" +
                    $"        return dict;{Environment.NewLine}" +
                    $"    }}{Environment.NewLine}" +
                    $"{Environment.NewLine}" +
                    $"    public void Hydrate(IDictionary<string,string> dict){Environment.NewLine}" +
                    $"    {{{Environment.NewLine}" +
                    $"        var obj = this;{Environment.NewLine}" +
                    $"        {Environment.NewLine}" +
                    $"{hydrateSb}{Environment.NewLine}" +                    
                    $"    }}{Environment.NewLine}" +
                    $"}}{Environment.NewLine}" +
                    $"}}";                
                Console.WriteLine(ret);
                return ret;
            }
            catch(Exception e)
            {
                throw e;
            }
            


        }

        public void Initialize(GeneratorInitializationContext context)
        {
            
            //throw new NotImplementedException();
        }

        public void Execute(GeneratorExecutionContext context)
        {
            //if (!Debugger.IsAttached)
            //{
            //    Debugger.Launch();
            //}

            var attributeSymbol = context.Compilation.GetTypeByMetadataName(typeof(DocumentAttribute).FullName);            

            var classesWithAttributes = context.Compilation.SyntaxTrees.Where(st => st.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
                    .Any(p => p.DescendantNodes().OfType<AttributeSyntax>().Any()));
            foreach(var tree in classesWithAttributes)
            {
                var semanticModel = context.Compilation.GetSemanticModel(tree);
                if(semanticModel == null) continue;

                foreach(var declaredClass in tree
                                                .GetRoot()
                                                .DescendantNodes()
                                                .OfType<ClassDeclarationSyntax>()
                                                .Where(cd => cd.DescendantNodes().OfType<AttributeSyntax>().Any()))
                {
                    var nodes = declaredClass
                                    .DescendantNodes()
                                    .OfType<AttributeSyntax>()
                                    .FirstOrDefault(a => a.DescendantTokens().Any(dt => dt.IsKind(SyntaxKind.IdentifierToken) && dt.Parent != null && semanticModel.GetTypeInfo(dt.Parent).Type?.Name == attributeSymbol?.Name))
                                    ?.DescendantTokens()
                                    ?.Where(dt => dt.IsKind(SyntaxKind.IdentifierToken))
                                    ?.ToList();
                    if (nodes == null) continue;

                    var last = nodes.Last();
                    if(last.Parent == null) continue;

                    var relatedClass = semanticModel.GetTypeInfo(last.Parent);
                    var generatedClass = Generate(declaredClass);
                    context.AddSource($"{declaredClass.Identifier}_RedisGenerated", SourceText.From(generatedClass.ToString(), Encoding.UTF8));
                    

                }
            }
            //throw new NotImplementedException();
        }
    }
}
