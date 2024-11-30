using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace ConstAttribute.Analyzer
{
    [Generator]
    public class ConstAttributeSourceGenerator : IIncrementalGenerator
    {
        public const string DiagnosticDescriptorID = "CONST001";


        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Filtro i metodi con parametri che hanno l'attributo [Const]
            // e che hanno assegnazioni 
            var methodsWithConstParams = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: (node, _) => node is MethodDeclarationSyntax,
                    transform: (ctx, _) =>
                    {
                        MethodDeclarationSyntax methodDeclaration = (MethodDeclarationSyntax)ctx.Node;
                        SemanticModel semanticModel = ctx.SemanticModel;

                        // Ottengo i parametri con l'attributo [Const]
                        List<ParameterSyntax> constParams = methodDeclaration.ParameterList.Parameters
                            .Where(param => HasConstAttribute(param, semanticModel))
                            .ToList();

                        // Ottengo le modifiche alle proprietà dei parametri [Const]
                        List<(Location Location, string ParameterName)> propertyModifications = FindPropertyModifications(methodDeclaration, semanticModel, constParams);

                        return new { Method = methodDeclaration, Modifications = propertyModifications };
                    })
                .Where(x => x.Modifications.Any());

            // Genera errori di compilazione per le modifiche
            context.RegisterSourceOutput(methodsWithConstParams, (ctx, source) =>
            {
                foreach ((Location Location, string ParameterName) modification in source.Modifications)
                {
                    Diagnostic error = Diagnostic.Create(
                        new DiagnosticDescriptor(
                            DiagnosticDescriptorID,
                            DiagnosticStringsLocator.DiagnosticDescriptorTitle,
                            DiagnosticStringsLocator.DiagnosticDescriptorMessageFormat,
                            DiagnosticStringsLocator.DiagnosticDescriptorMessageCategory,
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        modification.Location,
                        modification.ParameterName);

                    ctx.ReportDiagnostic(error);
                }
            });
        }

        private bool HasConstAttribute(ParameterSyntax parameter, SemanticModel semanticModel)
        {
            ISymbol parameterSymbol = semanticModel.GetDeclaredSymbol(parameter);
            return parameterSymbol?.GetAttributes()
                .Any(attr => attr.AttributeClass?.Name == DiagnosticStringsLocator.ConstAttributeName) ?? false;
        }

        private List<(Location Location, string ParameterName)> FindPropertyModifications(
            MethodDeclarationSyntax methodDeclaration,
            SemanticModel semanticModel,
            List<ParameterSyntax> constParams)
        {
            List<(Location, string)> modifications = new List<(Location, string)>();

            // Cerco tutti i nodi di assegnazione nell'albero sintattico del metodo
            IEnumerable<AssignmentExpressionSyntax> assignmentNodes = methodDeclaration.DescendantNodes()
                .OfType<AssignmentExpressionSyntax>();

            foreach (AssignmentExpressionSyntax assignment in assignmentNodes)
            {
                // Controlla se l'assegnazione riguarda una proprietà di un parametro [Const]
                if (!(assignment.Left is MemberAccessExpressionSyntax memberAccess)) continue;

                SymbolInfo expressionSymbol = semanticModel.GetSymbolInfo(memberAccess.Expression);

                if (!(expressionSymbol.Symbol is IParameterSymbol parameterSymbol))
                {
                    continue;
                }

                ParameterSyntax matchingParam = constParams
                    .FirstOrDefault(p =>
                        semanticModel.GetDeclaredSymbol(p)?.Name == parameterSymbol.Name);

                if (matchingParam != null)
                {
                    modifications.Add((assignment.GetLocation(), parameterSymbol.Name));
                }
            }

            return modifications;
        }
    }
}
