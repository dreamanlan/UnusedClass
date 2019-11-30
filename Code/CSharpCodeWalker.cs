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
        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            if (Config.CollectClass) {
                var sym = m_Model.GetDeclaredSymbol(node);
                var fullName = SymbolInfo.CalcFullName(sym);
                var baseSym = sym.BaseType;
                if (Config.CanCollect(fullName, baseSym.Name, sym.AllInterfaces)) {
                    m_Identifiers[fullName] = string.Empty;
                }
            }
            base.VisitEnumDeclaration(node);
        }
        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            if (Config.CollectClass) {
                var sym = m_Model.GetDeclaredSymbol(node);
                var fullName = SymbolInfo.CalcFullName(sym);
                var baseSym = sym.BaseType;
                if (Config.CanCollect(fullName, baseSym.Name, sym.AllInterfaces)) {
                    m_Identifiers[fullName] = string.Empty;
                }
            }
            base.VisitStructDeclaration(node);
        }
        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (Config.CollectClass) {
                var sym = m_Model.GetDeclaredSymbol(node);
                var fullName = SymbolInfo.CalcFullName(sym);
                var baseSym = sym.BaseType;
                if (Config.CanCollect(fullName, baseSym.Name, sym.AllInterfaces)) {
                    m_Identifiers[fullName] = string.Empty;
                }
            }
            base.VisitClassDeclaration(node);
        }
        public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
        {
            if (Config.CollectField) {
                foreach (var v in node.Declaration.Variables) {
                    var sym = m_Model.GetDeclaredSymbol(v);
                    var fullName = SymbolInfo.CalcFullName(sym);
                    if (Config.CanCollect(fullName, string.Empty, System.Collections.Immutable.ImmutableArray<INamedTypeSymbol>.Empty)) {
                        m_Identifiers[fullName] = string.Empty;
                    }
                }
            }
            base.VisitEventFieldDeclaration(node);
        }
        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            if (Config.CollectField) {
                foreach (var v in node.Declaration.Variables) {
                    var sym = m_Model.GetDeclaredSymbol(v);
                    var fullName = SymbolInfo.CalcFullName(sym);
                    if (Config.CanCollect(fullName, string.Empty, System.Collections.Immutable.ImmutableArray<INamedTypeSymbol>.Empty)) {
                        m_Identifiers[fullName] = string.Empty;
                    }
                }
            }
            base.VisitFieldDeclaration(node);
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
        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            var sym = m_Model.GetDeclaredSymbol(node);
            var fullName = SymbolInfo.CalcFullName(sym);
            m_ClassStack.Push(fullName);
            base.VisitEnumDeclaration(node);
            m_ClassStack.Pop();
        }
        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            var sym = m_Model.GetDeclaredSymbol(node);
            var fullName = SymbolInfo.CalcFullName(sym);
            m_ClassStack.Push(fullName);
            base.VisitStructDeclaration(node);
            m_ClassStack.Pop();
        }
        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var sym = m_Model.GetDeclaredSymbol(node);
            var fullName = SymbolInfo.CalcFullName(sym);
            m_ClassStack.Push(fullName);
            base.VisitClassDeclaration(node);
            m_ClassStack.Pop();
        }
        public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
        {
            foreach (var v in node.Declaration.Variables) {
                var sym = m_Model.GetDeclaredSymbol(v);
                var fullName = SymbolInfo.CalcFullName(sym);
                m_MemberStack.Push(fullName);
            }
            base.VisitEventFieldDeclaration(node);
            foreach (var v in node.Declaration.Variables) {
                m_MemberStack.Pop();
            }
        }
        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            foreach (var v in node.Declaration.Variables) {
                var sym = m_Model.GetDeclaredSymbol(v);
                var fullName = SymbolInfo.CalcFullName(sym);
                m_MemberStack.Push(fullName);
            }
            base.VisitFieldDeclaration(node);
            foreach (var v in node.Declaration.Variables) {
                m_MemberStack.Pop();
            }
        }
        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            var symInfo = m_Model.GetSymbolInfo(node.Type);
            base.VisitObjectCreationExpression(node);
        }
        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            var oper = m_Model.GetOperation(node.Expression);
            base.VisitMemberAccessExpression(node);
        }
        public override void VisitGenericName(GenericNameSyntax node)
        {
            if (!node.IsVar && m_ClassStack.Count > 0) {
                var curClassName = m_ClassStack.Peek();
                var curMemberName = string.Empty;
                if (m_MemberStack.Count > 0) {
                    curMemberName = m_MemberStack.Peek();
                }
                var oper = m_Model.GetOperation(node);
                var symInfo = m_Model.GetSymbolInfo(node);
                var sym = symInfo.Symbol;
                if (null != sym) {
                    var name = SymbolInfo.CalcFullName(sym);
                    if (Config.CollectClass && sym.Kind == SymbolKind.NamedType && curClassName != name) {
                        bool mark = false;
                        if (m_Identifiers.ContainsKey(name) && Config.CanMark(curClassName)) {
                            m_Identifiers[name] = curClassName + ":" + node.GetLocation().GetLineSpan().ToString();
                            mark = true;
                        }
                        if (Config.NeedLog(name)) {
                            Console.WriteLine("[log] generic identifier:{0} kind:{1} code:{2} curclass:{3} mark:{4}", name, sym.Kind, node.Identifier.Text, curClassName, mark);
                        }
                    }
                    else if (Config.CollectField && (sym.Kind == SymbolKind.Event || sym.Kind == SymbolKind.Field) && curMemberName != name) {
                        bool mark = false;
                        if (m_Identifiers.ContainsKey(name)) {
                            m_Identifiers[name] = curClassName + ":" + node.GetLocation().GetLineSpan().ToString();
                            mark = true;
                        }
                        if (Config.NeedLog(name)) {
                            Console.WriteLine("[log] generic identifier:{0} kind:{1} code:{2} curclass:{3} curmember:{4} mark:{5}", name, sym.Kind, node.Identifier.Text, curClassName, curMemberName, mark);
                        }
                    }
                    else if (Config.NeedLog(name)) {
                        Console.WriteLine("[log] generic identifier:{0} kind:{1} code:{2} curclass:{3} curmember:{4}", name, sym.Kind, node.Identifier.Text, curClassName, curMemberName);
                    }
                }
                else if (null != oper) {
                    var name = SymbolInfo.CalcFullName(oper.Type);
                    Console.WriteLine("[warning] generic identifier:{0} invalid:{1} code:{2} curclass:{3} curmember:{4}", name, oper.IsInvalid, node.Identifier.Text, curClassName, curMemberName);
                }
            }
            base.VisitGenericName(node);
        }
        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (!node.IsVar && m_ClassStack.Count > 0) {
                var curClassName = m_ClassStack.Peek();
                var curMemberName = string.Empty;
                if (m_MemberStack.Count > 0) {
                    curMemberName = m_MemberStack.Peek();
                }
                var oper = m_Model.GetOperation(node);
                var symInfo = m_Model.GetSymbolInfo(node);
                var sym = symInfo.Symbol;
                if (null != sym) {
                    var name = SymbolInfo.CalcFullName(sym);
                    //name != MessageDefine.Cs2LuaMessageEnum2Object
                    if (Config.CollectClass && sym.Kind == SymbolKind.NamedType && curClassName != name) {
                        bool mark = false;
                        if (m_Identifiers.ContainsKey(name) && Config.CanMark(curClassName)) {
                            m_Identifiers[name] = curClassName + ":" + node.GetLocation().GetLineSpan().ToString();
                            mark = true;
                        }
                        if (Config.NeedLog(name)) {
                            Console.WriteLine("[log] identifier:{0} kind:{1} code:{2} curclass:{3} mark:{4}", name, sym.Kind, node.Identifier.Text, curClassName, mark);
                        }
                    }
                    else if (Config.CollectField && (sym.Kind == SymbolKind.Event || sym.Kind == SymbolKind.Field) && curMemberName != name) {
                        bool mark = false;
                        if (m_Identifiers.ContainsKey(name)) {
                            m_Identifiers[name] = curClassName + ":" + node.GetLocation().GetLineSpan().ToString();
                            mark = true;
                        }
                        if (Config.NeedLog(name)) {
                            Console.WriteLine("[log] identifier:{0} kind:{1} code:{2} curclass:{3} curmember:{4} mark:{5}", name, sym.Kind, node.Identifier.Text, curClassName, curMemberName, mark);
                        }
                    }
                    else if ((Config.CollectClass && curClassName != name || Config.CollectField && curMemberName != name || !Config.CollectClass && !Config.CollectField) && Config.NeedLog(name)) {
                        Console.WriteLine("[log] identifier:{0} kind:{1} code:{2} curclass:{3} curmember:{4}", name, sym.Kind, node.Identifier.Text, curClassName, curMemberName);
                    }
                }
                else if (null != oper) {
                    var name = SymbolInfo.CalcFullName(oper.Type);
                    Console.WriteLine("[warning] identifier:{0} invalid:{1} code:{2} curclass:{3} curmember:{4}", name, oper.IsInvalid, node.Identifier.Text, curClassName, curMemberName);
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
        private Stack<string> m_MemberStack = new Stack<string>();
    }
}
