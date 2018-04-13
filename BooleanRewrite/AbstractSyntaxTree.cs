﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace BooleanRewrite
{
    static class AST
    {
        const char _lnot = '\u00ac';
        const char _land = '\u2227';
        const char _lor = '\u2228';

        public static BoolExpr Make(ref List<Token>.Enumerator polishNotationTokensEnumerator)
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
            }
            return null;
        }

        public static string PrettyPrint(BoolExpr root)
        {
            StringBuilder output = new StringBuilder();
            PrettyPrintHelper(ref output, root);
            if (Char.IsWhiteSpace(output[0]))
            {
                output.Remove(0, 1);
            }
            return output.ToString();
        }


        static void PrettyPrintHelper(ref StringBuilder stringBuilder, BoolExpr node)
        {
            if (node.Op == BoolExpr.BOP.LEAF)
            {
                stringBuilder.Append(node.Lit);
            }
            else if (node.Op == BoolExpr.BOP.NOT)
            {
                stringBuilder.Append(_lnot);
                PrettyPrintHelper(ref stringBuilder, node.Right);
            }
            else if (node.Op == BoolExpr.BOP.AND)
            {
                stringBuilder.Append('(');
                PrettyPrintHelper(ref stringBuilder, node.Left);
                stringBuilder.Append(_land);
                PrettyPrintHelper(ref stringBuilder, node.Right);
                stringBuilder.Append(')');
            }
            else if (node.Op == BoolExpr.BOP.OR)
            {
                stringBuilder.Append('(');
                PrettyPrintHelper(ref stringBuilder, node.Left);
                stringBuilder.Append(_lor);
                PrettyPrintHelper(ref stringBuilder, node.Right);
                stringBuilder.Append(')');
            }
        }

        public static IList<ConversionStep> Evaluate(ref BoolExpr root)
        {
            var steps = new List<ConversionStep>();
            steps.Add(new ConversionStep(PrettyPrint(root), "Input"));

            if(!IsNNF(root))
            {
                ConvertToNNF(ref root, steps, ref root);
            }
            Debug.Assert(IsNNF(root));

            if (!IsDNF(root))
            {
                ConvertNNFtoDNF(ref root, steps, ref root);
            }
            Debug.Assert(IsDNF(root));

            return steps;
        }

        static void ConvertToNNF(ref BoolExpr node, IList<ConversionStep> steps, ref BoolExpr root)
        {
            if (node == null || node.Op == BoolExpr.BOP.LEAF)
                return;

            // try to apply DN as many times as possible
            while(Rewrite.DN(ref node, ref root))
            {
                steps.Add(new ConversionStep(PrettyPrint(node), "Double Negation"));
            }

            // try to apply DeM
            if(Rewrite.DeM(ref node, ref root))
            {
                steps.Add(new ConversionStep(PrettyPrint(node), "DeMorgan's"));
            }


            var right = node.Right;
            var left = node.Left;
            ConvertToNNF(ref right, steps, ref root);
            ConvertToNNF(ref left, steps, ref root);
            node.Right = right;
            node.Left = left;
        }

        static void ConvertNNFtoDNF(ref BoolExpr node, IList<ConversionStep> steps, ref BoolExpr root)
        {
            if (node == null || node.Op == BoolExpr.BOP.LEAF)
                return;

            // try to apply distribution
            if (Rewrite.Distribution(ref node, ref root))
            {
                steps.Add(new ConversionStep(PrettyPrint(node), "Distribution"));
            }

            var right = node.Right;
            var left = node.Left;
            ConvertNNFtoDNF(ref right, steps, ref root);
            ConvertNNFtoDNF(ref left, steps, ref root);
            node.Right = right;
            node.Left = left;
        }

        static bool IsDNF(BoolExpr node)
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


        static bool IsNNF(BoolExpr node)
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

    static class Rewrite
    {
        public static bool Distribution(ref BoolExpr node, ref BoolExpr root)
        {
            if (node.Op == BoolExpr.BOP.AND)
            {
                if (node.Right.Op == BoolExpr.BOP.OR)
                {
                    var oldRight = node.Right;
                    var oldLeft = node.Left;

                    var left = BoolExpr.CreateAnd(oldLeft, oldRight.Left);
                    var right = BoolExpr.CreateAnd(oldLeft, oldRight.Right);

                    node = BoolExpr.CreateOr(left, right);
                    left.Parent = node;
                    right.Parent = node;
                    if (node.Parent == null)
                    {
                        root = node;
                    }
                    return true;
                }
                else if (node.Left.Op == BoolExpr.BOP.OR)
                {
                    var oldRight = node.Right;
                    var oldLeft = node.Left;

                    var left = BoolExpr.CreateAnd(oldLeft.Left, oldRight);
                    var right = BoolExpr.CreateAnd(oldLeft.Right, oldRight);

                    node = BoolExpr.CreateOr(left, right);
                    left.Parent = node;
                    right.Parent = node;
                    if (node.Parent == null)
                    {
                        root = node;
                    }
                    return true;
                }
            }
            return false;
        }

        public static bool DeM(ref BoolExpr node, ref BoolExpr root)
        {
            if(node.Op == BoolExpr.BOP.NOT)
            {
                if (node.Right.Op == BoolExpr.BOP.AND)
                {
                    var temp = node.Right;
                    var left = BoolExpr.CreateNot(temp.Left);
                    var right = BoolExpr.CreateNot(temp.Right);
                    node = BoolExpr.CreateOr(left, right);
                    left.Parent = node;
                    right.Parent = node;
                    if (node.Parent == null)
                    {
                        root = node;
                    }
                    return true;
                }
                else if (node.Right.Op == BoolExpr.BOP.OR)
                {
                    var temp = node.Right;
                    var left = BoolExpr.CreateNot(temp.Left);
                    var right = BoolExpr.CreateNot(temp.Right);
                    node = BoolExpr.CreateAnd(left, right);
                    left.Parent = node;
                    right.Parent = node;
                    if (node.Parent == null)
                    {
                        root = node;
                    }
                    return true;
                }
            }
            return false;
        }

        public static bool DN(ref BoolExpr node, ref BoolExpr root)
        {
            if(node.Op == BoolExpr.BOP.NOT && node.Right.Op == BoolExpr.BOP.NOT)
            {
                var old_parent = node.Parent;
                node = node.Right.Right;
                node.Parent = old_parent;
                if (node.Parent == null)
                {
                    root = node;
                }
                return true;
            }
            return false;
        }
    }
}
