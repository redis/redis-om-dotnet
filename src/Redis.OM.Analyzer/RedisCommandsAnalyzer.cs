using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Redis.OM.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RedisCommandsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "RedisCommandsAnalyzer";

        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, "Commands not found", "Commands not found", Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: "Commands not found");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }
        static ImmutableHashSet<string> commands = ImmutableHashSet<string>.Empty;

        public override void Initialize(AnalysisContext context)
        {
            var json = File.ReadAllLines("/Users/aviavni/Repos/Redis.OM/src/Redis.OM.Analyzer/commands.json");
            commands = json
                .Where(x=> x.Contains("\"name\":"))
                .Select(x => x.Split(':')[1].Replace("\"", "").Replace(",", "").Trim().ToLower())
                .ToImmutableHashSet();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;
            if(!namedTypeSymbol.Name.Contains("RedisCommands")) return;

            foreach (var item in namedTypeSymbol.GetMembers())
            {
                var symbol = (IMethodSymbol)item;
                if(commands.Contains(symbol.Name.ToLower())) continue;

                // For all such symbols, produce a diagnostic.
                var diagnostic = Diagnostic.Create(Rule, item.Locations[0]);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}