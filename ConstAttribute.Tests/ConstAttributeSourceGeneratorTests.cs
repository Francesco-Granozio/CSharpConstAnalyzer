using ConstAttribute.Analyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConstAttribute.Tests
{
    [TestClass]
    public class ConstAttributeSourceGeneratorTests
    {
        [TestMethod]
        public async Task TestViolationWithSourceGenerator()
        {
            string code = @"
using System;

public static class Program 
{
    public class SomeClass
    {
        public string SomeProperty { get; set; }
    }

    public static void Main() 
    {
        SomeClass s = new SomeClass();
        TestMethod(s);
    }

    public static void TestMethod([Const] SomeClass obj)
    {
        obj.SomeProperty = ""Modified"";  // Questo dovrebbe generare un errore
    }

    public class ConstAttribute : Attribute { }
}";

            DiagnosticInfo diagnosticInfo = await Utilities.GetDiagnosticInfo(code);

            ConstAttributeSourceGenerator generator = new ConstAttributeSourceGenerator();

            // Recupero il progetto dal workspace
            Project project = diagnosticInfo.Workspace.CurrentSolution.GetProject(diagnosticInfo.Document.Project.Id);

            // Eseguo il generatore con il compilatore
            Compilation compilation = await project.GetCompilationAsync();

            // Configuro il driver per eseguire il generatore
            CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
            driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

            GeneratorDriverRunResult results = driver.GetRunResult();

            // Controllo che ci sia almeno un errore con ID "CONST001"
            Assert.IsTrue(results.Diagnostics.Any(d => d.Id == ConstAttributeSourceGenerator.DiagnosticDescriptorID), 
                $"Expected diagnostic '{ConstAttributeSourceGenerator.DiagnosticDescriptorID}' was not reported.");
        }



        [TestMethod]
        public async Task FixRemoveConstAttribute()
        {
            //Codice con errore 
            string testCode = @"
using System;

public static class Program 
{
    public class SomeClass
    {
        public string SomeProperty { get; set; }
    }

    public static void Main() 
    {
        SomeClass s1 = new SomeClass();
        SomeClass s2 = new SomeClass();

        TestMethod(s1, s2);
    }

    public static void TestMethod([Const] SomeClass s1, [Const] SomeClass s2)
    {
        s1.SomeProperty = ""Modified"";  
    }

    public class ConstAttribute : Attribute { }
}";

            // Codice fixato
            string fixedCode = @"
using System;

public static class Program 
{
    public class SomeClass
    {
        public string SomeProperty { get; set; }
    }

    public static void Main() 
    {
        SomeClass s1 = new SomeClass();
        SomeClass s2 = new SomeClass();

        TestMethod(s1, s2);
    }

    public static void TestMethod(SomeClass s1, [Const] SomeClass s2)
    {
        s1.SomeProperty = ""Modified"";  
    }

    public class ConstAttribute : Attribute { }
}";

            DiagnosticInfo diagnosticInfo = await Utilities.GetDiagnosticInfo(testCode);

            // Deve esserc solo una diagnostica
            Assert.AreEqual(1, diagnosticInfo.Diagnostics.Length);

            Diagnostic diagnostic = diagnosticInfo.Diagnostics[0];

            ConstAttributeCodeFixProvider codeFixProvider = new ConstAttributeCodeFixProvider();

            CodeAction registeredCodeAction = null;

            // Creo il contesto per registrare il CodeFix
            CodeFixContext context = new CodeFixContext(diagnosticInfo.Document, diagnostic, (codeAction, _) =>
            {
                if (registeredCodeAction != null)
                    throw new Exception("Code action was registered more than once");

                registeredCodeAction = codeAction;
            }, CancellationToken.None);

            // Registro il CodeFix
            await codeFixProvider.RegisterCodeFixesAsync(context);

            // Verifico che il code fix sia stato registrato
            if (registeredCodeAction == null)
                throw new Exception("Code action was not registered");

            // Ottiengo le operazioni da eseguire
            System.Collections.Immutable.ImmutableArray<CodeActionOperation> operations = await registeredCodeAction.GetOperationsAsync(CancellationToken.None);

            // Applico le operazioni (modifica del codice)
            foreach (CodeActionOperation operation in operations)
            {
                operation.Apply(diagnosticInfo.Workspace, CancellationToken.None);
            }

            // Ottiengo il documento aggiornato
            Document updatedDocument = diagnosticInfo.Workspace.CurrentSolution.GetDocument(diagnosticInfo.Document.Id);
            string newCode = (await updatedDocument.GetTextAsync()).ToString();

            // Verifico che il codice modificato sia corretto
            Assert.AreEqual(fixedCode, newCode);
        }


    }
}
