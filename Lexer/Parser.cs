using System;
using System.Collections.Generic;
using System.Linq;

namespace Lexer
{
    public class RecordTypeParser
    {
        public enum TokenType
        {
            KeywordType,
            Identifier,
            KeywordRecord,
            Comma,
            Colon,
            Semicolon,
            KeywordEnd,
            TypeKeyword,
            Equals,
            Unknown,
            EOF
        }

        public class Token
        {
            public TokenType Type { get; set; }
            public string Value { get; set; }
            public int Line { get; set; }
            public int Position { get; set; }

            public Token(TokenType type, string value, int line, int position)
            {
                Type = type;
                Value = value;
                Line = line;
                Position = position;
            }
        }

        public class ParseError
        {
            public string Message { get; set; }
            public int Line { get; set; }
            public int Position { get; set; }
            public string Fragment { get; set; }

            public ParseError(string message, int line, int position, string fragment)
            {
                Message = message;
                Line = line;
                Position = position;
                Fragment = fragment;
            }
        }

        private string input;
        private int position;
        private int line;
        private int lineStartPosition;
        private List<Token> tokens;
        private List<ParseError> errors;

        private static readonly HashSet<string> TypeKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "real", "integer", "boolean", "char", "string"
        };

        public RecordTypeParser(string input)
        {
            this.input = input;
            position = 0;
            line = 1;
            lineStartPosition = 0;
            tokens = new List<Token>();
            errors = new List<ParseError>();
        }

        public bool Parse()
        {
            Tokenize();
            return ParseTypeDefinitions();
        }

        public List<ParseError> GetErrors()
        {
            return errors;
        }

        private void Tokenize()
        {
            while (position < input.Length)
            {
                char current = input[position];

                if (char.IsWhiteSpace(current))
                {
                    if (current == '\n')
                    {
                        line++;
                        lineStartPosition = position + 1;
                    }
                    position++;
                    continue;
                }

                if (char.IsLetter(current))
                {
                    string word = ReadWord();
                    TokenType type = GetKeywordType(word);
                    tokens.Add(new Token(type, word, line, position - lineStartPosition + 1));
                    continue;
                }

                if (current == ',')
                {
                    tokens.Add(new Token(TokenType.Comma, ",", line, position - lineStartPosition + 1));
                    position++;
                    continue;
                }

                if (current == ':')
                {
                    tokens.Add(new Token(TokenType.Colon, ":", line, position - lineStartPosition + 1));
                    position++;
                    continue;
                }

                if (current == ';')
                {
                    tokens.Add(new Token(TokenType.Semicolon, ";", line, position - lineStartPosition + 1));
                    position++;
                    continue;
                }

                if (current == '=')
                {
                    tokens.Add(new Token(TokenType.Equals, "=", line, position - lineStartPosition + 1));
                    position++;
                    continue;
                }

                tokens.Add(new Token(TokenType.Unknown, current.ToString(), line, position - lineStartPosition + 1));
                position++;
            }

            tokens.Add(new Token(TokenType.EOF, "", line, position - lineStartPosition + 1));
        }

        private string ReadWord()
        {
            int start = position;
            while (position < input.Length && char.IsLetterOrDigit(input[position]))
            {
                position++;
            }
            return input.Substring(start, position - start);
        }

        private TokenType GetKeywordType(string word)
        {
            switch (word.ToLower())
            {
                case "type": return TokenType.KeywordType;
                case "record": return TokenType.KeywordRecord;
                case "end": return TokenType.KeywordEnd;
                default:
                    return TypeKeywords.Contains(word) ? TokenType.TypeKeyword : TokenType.Identifier;
            }
        }

        private bool ParseTypeDefinitions()
        {
            int currentToken = 0;
            bool hasValidType = false;

            while (currentToken < tokens.Count && tokens[currentToken].Type != TokenType.EOF)
            {
                if (tokens[currentToken].Type == TokenType.KeywordType)
                {
                    if (!ParseSingleTypeDefinition(ref currentToken))
                    {
                        return false;
                    }
                    hasValidType = true;
                }
                else
                {
                    var token = tokens[currentToken];
                    AddError($"Неожиданный токен '{token.Value}', ожидается 'type'", token.Line, token.Position, token.Value);
                    return false;
                }
            }

            if (!hasValidType)
            {
                AddError("Не найдено ни одного определения типа", 1, 1, "");
                return false;
            }

            return true;
        }

        private bool ParseSingleTypeDefinition(ref int currentToken)
        {
            // type <имя> = record ... end;
            if (!CheckToken(ref currentToken, TokenType.KeywordType, "Ожидается ключевое слово 'type'"))
                return false;

            if (!CheckToken(ref currentToken, TokenType.Identifier, "Ожидается имя типа"))
                return false;

            if (!CheckToken(ref currentToken, TokenType.Equals, "Ожидается символ '='"))
                return false;

            if (!CheckToken(ref currentToken, TokenType.KeywordRecord, "Ожидается ключевое слово 'record'"))
                return false;

            // Обрабатываем поля записи
            bool hasFields = false;
            while (currentToken < tokens.Count &&
                   tokens[currentToken].Type != TokenType.KeywordEnd &&
                   tokens[currentToken].Type != TokenType.KeywordType)
            {
                if (!ParseFieldDeclaration(ref currentToken))
                    return false;
                hasFields = true;
            }

            if (!hasFields)
            {
                AddError("Запись должна содержать хотя бы одно поле", tokens[currentToken].Line, tokens[currentToken].Position, "");
                return false;
            }

            // Проверяем завершение: end;
            if (!CheckToken(ref currentToken, TokenType.KeywordEnd, "Ожидается ключевое слово 'end'"))
                return false;

            if (!CheckToken(ref currentToken, TokenType.Semicolon, "Ожидается символ ';' после 'end'"))
                return false;

            return true;
        }

        private bool ParseFieldDeclaration(ref int currentToken)
        {
            // Обрабатываем список идентификаторов
            if (!CheckToken(ref currentToken, TokenType.Identifier, "Ожидается имя поля"))
                return false;

            while (CheckToken(ref currentToken, TokenType.Comma, ""))
            {
                if (!CheckToken(ref currentToken, TokenType.Identifier, "Ожидается имя поля после запятой"))
                    return false;
            }

            // Обрабатываем тип поля
            if (!CheckToken(ref currentToken, TokenType.Colon, "Ожидается символ ':' после списка полей"))
                return false;

            if (!CheckToken(ref currentToken, TokenType.TypeKeyword, "Ожидается тип поля (real, integer и т.д.)"))
                return false;

            // Точка с запятой не обязательна после последнего поля
            if (currentToken < tokens.Count &&
                tokens[currentToken].Type == TokenType.Semicolon &&
                (currentToken + 1 >= tokens.Count ||
                 tokens[currentToken + 1].Type != TokenType.KeywordEnd &&
                 tokens[currentToken + 1].Type != TokenType.KeywordType))
            {
                currentToken++;
            }

            return true;
        }

        private bool CheckToken(ref int currentToken, TokenType expectedType, string errorMessage)
        {
            if (currentToken >= tokens.Count)
            {
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    AddError(errorMessage, line, position - lineStartPosition + 1, "");
                }
                return false;
            }

            var token = tokens[currentToken];
            if (token.Type != expectedType)
            {
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    AddError(errorMessage, token.Line, token.Position, token.Value);
                }
                return false;
            }

            currentToken++;
            return true;
        }

        private void AddError(string message, int line, int position, string fragment)
        {
            errors.Add(new ParseError(message, line, position, fragment));
        }
    }
}