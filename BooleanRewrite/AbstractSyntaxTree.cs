﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace BooleanRewrite
{
    /// <summary>
    /// Abstract Syntax Tree
    /// </summary>
    public class AST
    {
        public AST(List<Token> tokens)
        {
            var enumerator = tokens.GetEnumerator();
            enumerator.MoveNext();
            Root = Make(ref enumerator);
        }

        public BoolExpr Root
        {
            get;
            set;
        }

        BoolExpr Make(ref List<Token>.Enumerator polishNotationTokensEnumerator)
        {
            if (polishNotationTokensEnumerator.Current.type == Token.TokenType.LITERAL)
            {
                BoolExpr lit = BoolExpr.CreateBoolVar(polishNotationTokensEnumerator.Current.value);
                polishNotationTokensEnumerator.MoveNext();
                return lit;
            }
            else
            {
                if (polishNotationTokensEnumerator.Current.value == "NOT")
                {
                    polishNotationTokensEnumerator.MoveNext();
                    BoolExpr operand = Make(ref polishNotationTokensEnumerator);
                    var parent = BoolExpr.CreateNot(operand);
                    operand.Parent = parent;
                    return parent;
                }
                else if (polishNotationTokensEnumerator.Current.value == "AND")
                {
                    polishNotationTokensEnumerator.MoveNext();
                    BoolExpr right = Make(ref polishNotationTokensEnumerator);
                    BoolExpr left = Make(ref polishNotationTokensEnumerator);
                    var parent = BoolExpr.CreateAnd(left, right);
                    left.Parent = parent;
                    right.Parent = parent;
                    return parent;
                }
                else if (polishNotationTokensEnumerator.Current.value == "OR")
                {
                    polishNotationTokensEnumerator.MoveNext();
                    BoolExpr right = Make(ref polishNotationTokensEnumerator);
                    BoolExpr left = Make(ref polishNotationTokensEnumerator);
                    var parent = BoolExpr.CreateOr(left, right);
                    left.Parent = parent;
                    right.Parent = parent;
                    return parent;
                }
                else if (polishNotationTokensEnumerator.Current.value == "CONDITIONAL")
                {
                    polishNotationTokensEnumerator.MoveNext();
                    BoolExpr right = Make(ref polishNotationTokensEnumerator);
                    BoolExpr left = Make(ref polishNotationTokensEnumerator);
                    var parent = BoolExpr.CreateConditional(left, right);
                    left.Parent = parent;
                    right.Parent = parent;
                    return parent;
                }
                else if (polishNotationTokensEnumerator.Current.value == "BICONDITIONAL")
                {
                    polishNotationTokensEnumerator.MoveNext();
                    BoolExpr right = Make(ref polishNotationTokensEnumerator);
                    BoolExpr left = Make(ref polishNotationTokensEnumerator);
                    var parent = BoolExpr.CreateBiconditional(left, right);
                    left.Parent = parent;
                    right.Parent = parent;
                    return parent;
                }
                else if (polishNotationTokensEnumerator.Current.value == "XOR")
                {
                    polishNotationTokensEnumerator.MoveNext();
                    BoolExpr right = Make(ref polishNotationTokensEnumerator);
                    BoolExpr left = Make(ref polishNotationTokensEnumerator);
                    var parent = BoolExpr.CreateXor(left, right);
                    left.Parent = parent;
                    right.Parent = parent;
                    return parent;
                }
            }
            return null;
        }

        public override string ToString()
        {
            StringBuilder output = new StringBuilder();

            PrettyPrintHelper(output, Root);
            if (Char.IsWhiteSpace(output[0]))
            {
                output.Remove(0, 1);
            }
            return output.ToString();
        }

        void PrettyPrintHelper(StringBuilder stringBuilder, BoolExpr node)
        {
            if (node.Op == BoolExpr.BOP.LEAF)
            {
                stringBuilder.Append(node.Lit);
            }
            else if (node.Op == BoolExpr.BOP.NOT)
            {
                stringBuilder.Append(LogicalSymbols.Not);
                PrettyPrintHelper(stringBuilder, node.Right);
            }
            else if (node.Op == BoolExpr.BOP.AND)
            {
                stringBuilder.Append('(');
                PrettyPrintHelper(stringBuilder, node.Left);
                stringBuilder.Append(LogicalSymbols.And);
                PrettyPrintHelper(stringBuilder, node.Right);
                stringBuilder.Append(')');
            }
            else if (node.Op == BoolExpr.BOP.OR)
            {
                stringBuilder.Append('(');
                PrettyPrintHelper(stringBuilder, node.Left);
                stringBuilder.Append(LogicalSymbols.Or);
                PrettyPrintHelper(stringBuilder, node.Right);
                stringBuilder.Append(')');
            }
            else if (node.Op == BoolExpr.BOP.CONDITIONAL)
            {
                stringBuilder.Append('(');
                PrettyPrintHelper(stringBuilder, node.Left);
                stringBuilder.Append(LogicalSymbols.Conditional);
                PrettyPrintHelper(stringBuilder, node.Right);
                stringBuilder.Append(')');
            }
            else if (node.Op == BoolExpr.BOP.BICONDITIONAL)
            {
                stringBuilder.Append('(');
                PrettyPrintHelper(stringBuilder, node.Left);
                stringBuilder.Append(LogicalSymbols.Biconditional);
                PrettyPrintHelper(stringBuilder, node.Right);
                stringBuilder.Append(')');
            }
            else if (node.Op == BoolExpr.BOP.XOR)
            {
                stringBuilder.Append('(');
                PrettyPrintHelper(stringBuilder, node.Left);
                stringBuilder.Append(LogicalSymbols.XOr);
                PrettyPrintHelper(stringBuilder, node.Right);
                stringBuilder.Append(')');
            }
        }

        public IList<ConversionStep> Evaluate(IEnumerable<string> variables, bool reverse = false)
        {
            BoolExpr root;
            var steps = new ObservableCollection<ConversionStep>();
            steps.Add(new ConversionStep(ToString(), "Input"));

            if(!BasicOperators(Root))
            {
                root = Root;
                ConvertOperators(ref root, steps);
                Root = root;
            }
            Debug.Assert(BasicOperators(Root));

            if(!IsNNF(Root))
            {
                root = Root;
                ConvertToNNF(ref root, steps);
                Root = root;
            }
            Debug.Assert(IsNNF(Root));

            if (!IsDNF(Root))
            {
                root = Root;
                ConvertNNFtoDNF(ref root, steps);
                Root = root;
            }
            Debug.Assert(IsDNF(Root));

            // convert to DNFList
            var d = new DNFExpression(Root, variables);
            steps.Add(new ConversionStep(d.ToString(), "Association"));
            d.ConvertToCDNF(steps, reverse);
            return steps;
        }

        void ConvertOperators(ref BoolExpr node, IList<ConversionStep> steps)
        {
            if (node == null || node.Op == BoolExpr.BOP.LEAF)
                return;

            if(Rewrite.Implication(ref node))
            {
                if (node.Parent == null)
                {
                    Root = node;
                }
                steps.Add(new ConversionStep(ToString(), "Implication"));
            }

            if(Rewrite.Equivalence(ref node))
            {
                if (node.Parent == null)
                {
                    Root = node;
                }
                steps.Add(new ConversionStep(ToString(), "Equivalence"));
            }

            if (Rewrite.XOR(ref node))
            {
                if (node.Parent == null)
                {
                    Root = node;
                }
                steps.Add(new ConversionStep(ToString(), "Xor"));
            }

            var right = node.Right;
            var left = node.Left;
            ConvertOperators(ref left, steps);
            ConvertOperators(ref right, steps);
            node.Right = right;
            node.Left = left;
        }

        void ConvertToNNF(ref BoolExpr node, IList<ConversionStep> steps)
        {
            if (node == null || node.Op == BoolExpr.BOP.LEAF)
                return;

            // try to apply DN as many times as possible
            while(Rewrite.DN(ref node))
            {
                if (node.Parent == null)
                {
                    Root = node;
                }
                steps.Add(new ConversionStep(ToString(), "Double Negation"));
            }

            // try to apply DeM
            if(Rewrite.DeM(ref node))
            {
                if (node.Parent == null)
                {
                    Root = node;
                }
                steps.Add(new ConversionStep(ToString(), "DeMorgan's"));
            }


            var right = node.Right;
            var left = node.Left;
            ConvertToNNF(ref left, steps);
            ConvertToNNF(ref right, steps);
            node.Right = right;
            node.Left = left;
        }

        void ConvertNNFtoDNF(ref BoolExpr root, IList<ConversionStep> steps)
        {
            while(!IsDNF(Root))
            {
                ApplyDistribution(ref root, steps);
            }
        }

        void ApplyDistribution(ref BoolExpr node, IList<ConversionStep> steps)
        {
            if (node == null || node.Op == BoolExpr.BOP.LEAF)
                return;

            // try to apply distribution
            if (Rewrite.Distribution(ref node))
            {
                if (node.Parent == null)
                {
                    Root = node;
                }
                steps.Add(new ConversionStep(ToString(), "Distribution"));
            }
            var right = node.Right;
            var left = node.Left;
            ApplyDistribution(ref left, steps);
            ApplyDistribution(ref right, steps);
            node.Right = right;
            node.Left = left;
        }

        bool BasicOperators(BoolExpr node)
        {
            switch (node.Op)
            {
                case BoolExpr.BOP.LEAF:
                    return true;
                case BoolExpr.BOP.AND:
                    return BasicOperators(node.Right) && BasicOperators(node.Left);
                case BoolExpr.BOP.OR:
                    return BasicOperators(node.Right) && BasicOperators(node.Left);
                case BoolExpr.BOP.NOT:
                    return BasicOperators(node.Right);
                default:
                    return false;
            }
        }

        bool IsDNF(BoolExpr node)
        {
            switch (node.Op)
            {
                case BoolExpr.BOP.LEAF:
                    return true;
                case BoolExpr.BOP.AND:
                    return IsDNF(node.Right) && IsDNF(node.Left);
                case BoolExpr.BOP.OR:
                    return  (node.Parent == null || node.Parent.Op == BoolExpr.BOP.OR) && IsDNF(node.Right) && IsDNF(node.Left);
                case BoolExpr.BOP.NOT:
                    return node.Right.Op == BoolExpr.BOP.LEAF;
                default:
                    return false;
            }
        }

        bool IsNNF(BoolExpr node)
        {
            switch (node.Op)
            {
                case BoolExpr.BOP.LEAF:
                    return true;
                case BoolExpr.BOP.AND:
                case BoolExpr.BOP.OR:
                    return IsNNF(node.Right) && IsNNF(node.Left);
                case BoolExpr.BOP.NOT:
                    return node.Right.Op == BoolExpr.BOP.LEAF;
                default:
                    return false;
            }
        }
    }
}
