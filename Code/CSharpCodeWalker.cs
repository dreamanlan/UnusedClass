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
    internal class CSharpCodeCollecter : CSharpSyntaxWalker
    {
        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var sym = m_Model.GetDeclaredSymbol(node);
            var fullName = SymbolInfo.CalcFullName(sym, true);
            var baseSym = sym.BaseType;
            if(Config.CanCollect(fullName, baseSym.Name, sym.AllInterfaces)) {
                m_Identifiers[fullName] = string.Empty;
            }
            /*
            if (null != baseSym && (baseSym.Name == "IJceMessage" || baseSym.Name == "ICs2LuaJceMessage")) {
                //m_Identifiers[fullName] = string.Empty;
            } else {
                m_Identifiers[fullName] = string.Empty;
            }
            foreach (var intf in sym.AllInterfaces) {
                if (intf.Name == "IJceMessage" || intf.Name == "ICs2LuaJceMessage") {
                    //m_Identifiers[fullName] = string.Empty;
                } else {
                    m_Identifiers[fullName] = string.Empty;
                }
            }
            */
            base.VisitClassDeclaration(node);
        }

        public CSharpCodeCollecter(SemanticModel model, Dictionary<string, string> identifiers)
        {
            m_Model = model;
            m_Identifiers = identifiers;
        }

        private SemanticModel m_Model = null;
        private Dictionary<string, string> m_Identifiers = null;
    }
    internal class CSharpCodeMarker : CSharpSyntaxWalker
    {
        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var sym = m_Model.GetDeclaredSymbol(node);
            var fullName = SymbolInfo.CalcFullName(sym, true);
            m_ClassStack.Push(fullName);
            base.VisitClassDeclaration(node);
            m_ClassStack.Pop();
        }
        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (m_ClassStack.Count > 0) {
                var curClassName = m_ClassStack.Peek();
                var symInfo = m_Model.GetSymbolInfo(node);
                var sym = symInfo.Symbol as INamedTypeSymbol;
                if (null != sym) {
                    var name = SymbolInfo.CalcFullName(sym, true);
                    //name != MessageDefine.Cs2LuaMessageEnum2Object
                    if (Config.CanMark(name) && curClassName != name && m_Identifiers.ContainsKey(name)) {
                        m_Identifiers[name] = curClassName + ":" + node.GetLocation().GetLineSpan().ToString();
                    }
                }
            }
            base.VisitIdentifierName(node);
        }
        public CSharpCodeMarker(SemanticModel model, Dictionary<string, string> identifiers)
        {
            m_Model = model;
            m_Identifiers = identifiers;
        }

        private SemanticModel m_Model = null;
        private Dictionary<string, string> m_Identifiers = null;
        private Stack<string> m_ClassStack = new Stack<string>();
    }
}
