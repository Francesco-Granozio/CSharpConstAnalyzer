using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace ConstAttribute.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConstAttributeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticID = "ConstAttributeAnalyzer";
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticID,
            "Const Attribute Violation",
            "Parameter '{0}' marked with [Const] cannot have its properties modified",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Prevents modification of parameters marked with [Const] attribute.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Registro l'analisi del codice solo sull'assegnazione
            context.RegisterSyntaxNodeAction(
                AnalyzePropertyModification,
                SyntaxKind.SimpleAssignmentExpression
            );
        }

        private void AnalyzePropertyModification(SyntaxNodeAnalysisContext context)
        {
            // Filtro per le assegnazioni
            if (!(context.Node is AssignmentExpressionSyntax assignmentExpression))
                return;

            // Controllo se è un lvalue
            if (!(assignmentExpression.Left is MemberAccessExpressionSyntax memberAccessExpression))
                return;

            // Trovo il simbolo della variabile
            SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccessExpression.Expression);
            if (!(symbolInfo.Symbol is IParameterSymbol parameterSymbol))
                return;

            // Controllo se il parametro ha l'attributo [Const]
            bool hasConstAttribute = parameterSymbol.GetAttributes()
                .Any(attr => attr.AttributeClass.Name == nameof(ConstAttribute));

            if (hasConstAttribute)
            {
                // Genero un diagnostic per la modifica di un parametro [Const]
                var diagnostic = Diagnostic.Create(
                    Rule,
                    assignmentExpression.GetLocation(),
                    parameterSymbol.Name
                );
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
