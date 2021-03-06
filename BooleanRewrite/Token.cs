﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BooleanRewrite
{
    /// <summary>
    /// Represents parsing tokens
    /// </summary>
    public class Token
    {
        static Dictionary<char, KeyValuePair<TokenType, string>> dict = new Dictionary<char, KeyValuePair<TokenType, string>>()
        {
            {
                '(', new KeyValuePair<TokenType, string>(TokenType.OPEN_PAREN, "(")
            },
            {
                ')', new KeyValuePair<TokenType, string>(TokenType.CLOSE_PAREN, ")")
            },
            {
                LogicalSymbols.Not, new KeyValuePair<TokenType, string>(TokenType.NEGATION_OP, OperatorType.NOT.ToString())
            },
            {
                LogicalSymbols.And, new KeyValuePair<TokenType, string>(TokenType.BINARY_OP, OperatorType.AND.ToString())
            },
            {
                LogicalSymbols.Or, new KeyValuePair<TokenType, string>(TokenType.BINARY_OP, OperatorType.OR.ToString())
            },
            {
                LogicalSymbols.Conditional, new KeyValuePair<TokenType, string>(TokenType.BINARY_OP, OperatorType.CONDITIONAL.ToString())
            },
            {
                LogicalSymbols.Biconditional, new KeyValuePair<TokenType, string>(TokenType.BINARY_OP, OperatorType.BICONDITIONAL.ToString())
            },
            {
                LogicalSymbols.XOr, new KeyValuePair<TokenType, string>(TokenType.BINARY_OP, OperatorType.XOR.ToString())
            }
        };

        public enum TokenType
        {
            OPEN_PAREN,
            CLOSE_PAREN,
            NEGATION_OP,
            BINARY_OP,
            LITERAL,
            EXPR_END
        }


        public TokenType type;
        public string value;

        /// <summary>
        /// Regex used for text validation
        /// </summary>
        static Regex illegalRegex = new Regex($"[^a-zA-Z0-9()_-{LogicalSymbols.Operators}]");
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="s"></param>
        private Token(StringReader s)
        {
            int c = s.Read();
            if (c == -1)
            {
                type = TokenType.EXPR_END;
                value = "";
                return;
            }

            char ch = (char)c;

            if (dict.ContainsKey(ch))
            {
                type = dict[ch].Key;
                value = dict[ch].Value;
            }
            else
            {
                string str = "";
                str += ch;
                while (s.Peek() != -1 && !dict.ContainsKey((char)s.Peek()))
                {
                    str += (char)s.Read();
                }
                type = TokenType.LITERAL;
                value = str;
            }
        }

        /// <summary>
        /// Parses input returning a list of tokens
        /// </summary>
        /// <param name="text"></param>
        /// <param name="variables"></param>
        /// <returns></returns>
        public static List<Token> Tokenize(string text, IEnumerable<string> variables)
        {
            ValidateInput(text);

            List<Token> tokens = new List<Token>();
            StringReader reader = new StringReader(text);

            //Tokenize the expression
            Token t = null;
            do
            {
                t = new Token(reader);
                if(t.type == TokenType.LITERAL)
                {
                    if(!variables.Contains(t.value))
                    {
                        throw new IllegalVariableException($"Unexpected variable \"{t.value}\" found.");
                    }
                }
                tokens.Add(t);
            } while (t.type != Token.TokenType.EXPR_END);

            //Use a minimal version of the Shunting Yard algorithm to transform the token list to polish notation
            List<Token> polishNotation = TransformToPolishNotation(tokens);

            return polishNotation;
        }

        /// <summary>
        /// Validates input text
        /// </summary>
        /// <param name="text"></param>
        static void ValidateInput(string text)
        {
            var operators = LogicalSymbols.Operators;
            if(illegalRegex.IsMatch(text) || operators.Contains(text.LastOrDefault()))
            {
                throw new IllegalCharacterException();
            }
            if(text.Count(c => c=='(') != text.Count(c => c==')'))
            {
                throw new ParenthesisMismatchExeption();
            }
        }

        /// <summary>
        /// converts infix tokens to prefix tokens
        /// </summary>
        /// <param name="infixTokenList"></param>
        /// <returns></returns>
        static List<Token> TransformToPolishNotation(List<Token> infixTokenList)
        {
            Queue<Token> outputQueue = new Queue<Token>();
            Stack<Token> stack = new Stack<Token>();

            int index = 0;
            while (infixTokenList.Count > index)
            {
                Token t = infixTokenList[index];

                switch (t.type)
                {
                    case Token.TokenType.LITERAL:
                        outputQueue.Enqueue(t);
                        while(stack.Count > 0 && stack.Peek().type == TokenType.NEGATION_OP)
                        {
                            outputQueue.Enqueue(stack.Pop());
                        }
                        break;
                    case Token.TokenType.BINARY_OP:
                    case Token.TokenType.OPEN_PAREN:
                    case Token.TokenType.NEGATION_OP:
                        stack.Push(t);
                        break;
                    case Token.TokenType.CLOSE_PAREN:
                        while (stack.Peek().type != Token.TokenType.OPEN_PAREN)
                        {
                            outputQueue.Enqueue(stack.Pop());
                        }
                        stack.Pop();
                        while (stack.Count > 0 && stack.Peek().type == Token.TokenType.NEGATION_OP)
                        {
                            outputQueue.Enqueue(stack.Pop());
                        }
                        break;
                    default:
                        break;
                }

                ++index;
            }
            while (stack.Count > 0)
            {
                outputQueue.Enqueue(stack.Pop());
            }

            return outputQueue.Reverse().ToList();
        }

    }

    #region Exceptions
    [Serializable]
    internal class IllegalVariableException : Exception
    {
        public IllegalVariableException()
        {
        }

        public IllegalVariableException(string message) : base(message)
        {
        }

        public IllegalVariableException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected IllegalVariableException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    internal class ParenthesisMismatchExeption : Exception
    {
        public ParenthesisMismatchExeption()
        {
        }

        public ParenthesisMismatchExeption(string message) : base(message)
        {
        }

        public ParenthesisMismatchExeption(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ParenthesisMismatchExeption(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    internal class IllegalCharacterException : Exception
    {
        public IllegalCharacterException()
        {
        }

        public IllegalCharacterException(string message) : base(message)
        {
        }

        public IllegalCharacterException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected IllegalCharacterException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
    #endregion

}
