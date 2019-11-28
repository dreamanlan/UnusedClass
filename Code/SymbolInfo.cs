using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace RoslynTool
{
    internal static class SymbolInfo
    {
        internal static string CalcFullName(ISymbol type, bool includeSelfName)
        {
            if (null == type)
                return string.Empty;
            while (null != type && null != type.OriginalDefinition && type != type.OriginalDefinition) {
                type = type.OriginalDefinition;
            }
            List<string> list = new List<string>();
            if (includeSelfName) {
                list.Add(CalcNameWithTypeParameters(type));
            }
            INamespaceSymbol ns = type.ContainingNamespace;
            var ct = type.ContainingType;
            string name = string.Empty;
            if (null != ct) {
                name = CalcNameWithTypeParameters(ct);
            }
            while (null != ct && name.Length > 0) {
                list.Insert(0, name);
                ns = ct.ContainingNamespace;
                ct = ct.ContainingType;
                if (null != ct) {
                    name = CalcNameWithTypeParameters(ct);
                }
                else {
                    name = string.Empty;
                }
            }
            while (null != ns && ns.Name.Length > 0) {
                list.Insert(0, ns.Name);
                ns = ns.ContainingNamespace;
            }
            return string.Join(".", list.ToArray());
        }

        private static string CalcNameWithTypeParameters(ISymbol sym)
        {
            if (null == sym)
                return string.Empty;
            var typeSym = sym as INamedTypeSymbol;
            if (null != typeSym) {
                return CalcNameWithTypeParameters(typeSym);
            }
            else {
                return sym.Name;
            }
        }
        private static string CalcNameWithTypeParameters(INamedTypeSymbol type)
        {
            if (null == type)
                return string.Empty;
            List<string> list = new List<string>();
            list.Add(type.Name);
            foreach (var param in type.TypeParameters) {
                list.Add(param.Name);
            }
            return string.Join("_", list.ToArray());
        }
    }
}
