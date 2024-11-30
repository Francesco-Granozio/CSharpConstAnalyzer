using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace ConstAttribute.Analyzer
{
    internal static class DiagnosticStringsLocator
    {
        private static readonly ResourceManager m_ResourceManager = new ResourceManager(
            "ConstAttribute.Analyzer.Resources.DiagnosticStrings",
            Assembly.GetExecutingAssembly());


        internal static readonly string ConstAttributeName = nameof(ConstAttribute);
        internal static readonly string ConstAttributeShortName = nameof(ConstAttribute).Replace("Attribute", string.Empty);
        internal static string CodeFixTitle => GetString("CodeFixTitle");
        internal static string DiagnosticDescriptorMessageCategory => GetString("DiagnosticDescriptorMessageCategory");
        internal static string DiagnosticDescriptorMessageFormat => GetString("DiagnosticDescriptorMessageFormat");
        internal static string DiagnosticDescriptorTitle => GetString("DiagnosticDescriptorTitle");
        internal static string DiagnosticRuleTitle => GetString("DiagnosticRuleTitle");
        internal static string DiagnosticRuleMessageFormat => GetString("DiagnosticRuleMessageFormat");
        internal static string DiagnosticRuleDescription => GetString("DiagnosticRuleDescription");

        internal static string GetString(string key)
        {
            return m_ResourceManager.GetString(key);
        }
    }
}
