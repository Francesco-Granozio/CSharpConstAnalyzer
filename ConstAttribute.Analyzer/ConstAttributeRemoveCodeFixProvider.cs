using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ParameterSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ParameterSyntax;

namespace ConstAttribute.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConstAttributeCodeFixProvider)), Shared]
    public class ConstAttributeCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(ConstAttributeAnalyzer.DiagnosticDescriptorID);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Diagnostic diagnostic = context.Diagnostics.First();
            Microsoft.CodeAnalysis.Text.TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

            Document document = context.Document;
            CancellationToken cancellationToken = context.CancellationToken;

            // Ottiengo la root
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);
            if (root == null)
            {
                System.Diagnostics.Debug.WriteLine("Root is null");
                return;
            }

            // Trovo il nodo nell'area del diagnostic span
            SyntaxNode node = root.FindNode(diagnosticSpan);
            System.Diagnostics.Debug.WriteLine($"Found Node Kind: {node?.Kind()}");
            System.Diagnostics.Debug.WriteLine($"Found Node Text: {node?.ToString()}");

            // Se il nodo non è un'assegnazione non m interessa
            if (!(node is AssignmentExpressionSyntax assignmentNode))
            {
                return;
            }

            // Trovo il parametro del metodo che sta generando l'errore
            MethodDeclarationSyntax methodDeclaration = assignmentNode.Ancestors()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault();

            if (methodDeclaration == null)
            {
                System.Diagnostics.Debug.WriteLine("Method declaration not found.");
                return;
            }

            // Prendo il parametro annotato con [Const] all'interno del metodo
            ParameterSyntax parameterSyntax = methodDeclaration.ParameterList.Parameters
                .FirstOrDefault(p => p.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .Any(attr => attr.Name.ToString() == DiagnosticStringsLocator.ConstAttributeShortName));

            if (parameterSyntax == null)
            {
                System.Diagnostics.Debug.WriteLine($"Parameter with [{DiagnosticStringsLocator.ConstAttributeShortName}] not found.");
                return;
            }

            SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            // Registro il CodeFix per rimuovere l'attributo [Const]
            context.RegisterCodeFix(
                CodeAction.Create(
                    DiagnosticStringsLocator.CodeFixTitle,
                    c => RemoveConstAttributeAsync(document, parameterSyntax, c),
                    nameof(ConstAttributeCodeFixProvider)),
                diagnostic);
        }


        private async Task<Document> RemoveConstAttributeAsync(
            Document document,
            ParameterSyntax parameterSyntax,
            CancellationToken cancellationToken)
        {
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);

            // Rimuovo l'attributo [Const] dal parametro che da errore
            ParameterSyntax newParameterSyntax = parameterSyntax.RemoveNode(
                parameterSyntax.AttributeLists.First(),
                SyntaxRemoveOptions.KeepNoTrivia);

            // Sostituisco il parametro con il nuovo parametro senza l'attributo [Const]
            SyntaxNode newRoot = root.ReplaceNode(parameterSyntax, newParameterSyntax);

            // Documento aggiornato
            return document.WithSyntaxRoot(newRoot);
        }
    }


}