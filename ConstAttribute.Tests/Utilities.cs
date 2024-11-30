using ConstAttribute.Analyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConstAttribute.Tests
{
    public readonly struct DiagnosticInfo
    {
        public DiagnosticInfo(ImmutableArray<Diagnostic> diagnostics, Document document, Workspace workspace)
        {
            Diagnostics = diagnostics;
            Document = document;
            Workspace = workspace;
        }

        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public Document Document { get; }
        public Workspace Workspace { get; }
    }


    public static class Utilities
    {
        public static async Task<DiagnosticInfo> GetDiagnosticInfo(string code)
        {
            AdhocWorkspace workspace = new AdhocWorkspace();
            Solution solution = workspace.CurrentSolution;
            ProjectId projectId = ProjectId.CreateNewId();

            solution = solution
                .AddProject(
                    projectId,
                    "MyTestProject",
                    "MyTestProject",
                    LanguageNames.CSharp);

            DocumentId documentId = DocumentId.CreateNewId(projectId);

            solution = solution
                .AddDocument(documentId,
                "File.cs",
                code);

            Project project = solution.GetProject(projectId);

            project = project.AddMetadataReference(
                MetadataReference.CreateFromFile(
                    typeof(object).Assembly.Location))
                .AddMetadataReferences(GetAllReferencesNeededForType(typeof(ImmutableArray)));

            if (!workspace.TryApplyChanges(project.Solution))
                throw new Exception("Unable to apply changes to the workspace");

            Compilation compilation = await project.GetCompilationAsync();

            CompilationWithAnalyzers compilationWithAnalyzer = compilation.WithAnalyzers(
                ImmutableArray.Create<DiagnosticAnalyzer>(
                    new ConstAttributeAnalyzer()));

            ImmutableArray<Diagnostic> diagnostics = await compilationWithAnalyzer.GetAllDiagnosticsAsync();
            Document document = workspace.CurrentSolution.GetDocument(documentId);

            return new DiagnosticInfo(diagnostics, document, workspace);
        }

        public static MetadataReference[] GetAllReferencesNeededForType(Type type)
        {
            var files = GetAllAssemblyFilesNeededForType(type);

            return files.Select(x => MetadataReference.CreateFromFile(x)).Cast<MetadataReference>().ToArray();
        }

        public static ImmutableArray<string> GetAllAssemblyFilesNeededForType(Type type)
            => type.Assembly.GetReferencedAssemblies()
                .Select(x => Assembly.Load(x.FullName))
                .Append(type.Assembly)
                .Select(x => x.Location)
                .ToImmutableArray();

    }
}
