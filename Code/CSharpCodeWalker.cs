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
            var baseSym = sym.BaseType;
            if (null != baseSym && (baseSym.Name == "IJceMessage" || baseSym.Name == "ICs2LuaJceMessage")) {
                m_Identifiers[sym.Name] = false;
            }
            foreach (var intf in sym.AllInterfaces) {
                if (intf.Name == "IJceMessage" || intf.Name == "ICs2LuaJceMessage") {
                    m_Identifiers[sym.Name] = false;
                }
            }
            base.VisitClassDeclaration(node);
        }

        public CSharpCodeCollecter(SemanticModel model, Dictionary<string, bool> identifiers)
        {
            m_Model = model;
            m_Identifiers = identifiers;
        }

        private SemanticModel m_Model = null;
        private Dictionary<string, bool> m_Identifiers = null;
    }
    internal class CSharpCodeVerifier : CSharpSyntaxWalker
    {
        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            bool skip = false;
            var sym = m_Model.GetDeclaredSymbol(node);
            var baseSym = sym.BaseType;
            if (null != baseSym && (baseSym.Name == "IJceMessage" || baseSym.Name == "ICs2LuaJceMessage")) {
                skip = true;
            }
            foreach (var intf in sym.AllInterfaces) {
                if (intf.Name == "IJceMessage" || intf.Name == "ICs2LuaJceMessage") {
                    skip = true;
                }
            }
            if (skip)
                m_Skip = true;
            base.VisitClassDeclaration(node);
            if(skip)
                m_Skip = false;
        }
        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (m_Skip) {
            }
            else {
                var name = node.Identifier.Text;
                if (m_Identifiers.ContainsKey(name)) {
                    m_Identifiers[name] = true;
                }
            }
            base.VisitIdentifierName(node);
        }
        public CSharpCodeVerifier(SemanticModel model, Dictionary<string, bool> identifiers)
        {
            m_Model = model;
            m_Identifiers = identifiers;
        }

        private SemanticModel m_Model = null;
        private Dictionary<string, bool> m_Identifiers = null;
        private bool m_Skip = false;
    }
}
