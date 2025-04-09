using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Lexer
{
    public class RecordParser
    {
        private static readonly HashSet<string> Keywords = new HashSet<string> { "type", "record", "end", "real" };

        private enum TokenType { Keyword, Identifier, Symbol }

        private class Token
        {
            public TokenType Type;
            public string Value;
            public int Position;

            public Token(TokenType type, string value, int position)
            {
                Type = type;
                Value = value;
                Position = position;
            }
        }

        public List<string> ParseRecord(string input)
        {
            var tokens = Tokenize(input);
            var errors = new List<string>();
            int i = 0;

            // Улучшенная функция Expect - записывает ошибку, но продолжает разбор
            void Expect(string expected, string context)
            {
                if (i >= tokens.Count)
                {
                    errors.Add($"Ожидалось '{expected}' {context}, найдено 'EOF'");
                    return;
                }

                if (tokens[i].Value != expected)
                {
                    errors.Add($"Ожидалось '{expected}' {context}, найдено '{tokens[i].Value}'");
                    return;
                }

                i++;
            }

            // Функция для проверки идентификатора
            bool CheckIdentifier(string context)
            {
                if (i >= tokens.Count || tokens[i].Type != TokenType.Identifier)
                {
                    errors.Add($"Ожидался идентификатор {context}, найдено '{(i < tokens.Count ? tokens[i].Value : "EOF")}'");
                    return false;
                }
                return true;
            }

            // Основной алгоритм разбора
            try
            {
                // 1. Проверяем начало объявления
                Expect("type", "в начале объявления");

                // 2. Проверяем имя типа
                if (CheckIdentifier("после 'type'")) i++;

                // 3. Проверяем знак равенства
                Expect("=", "после имени типа");

                // 4. Проверяем ключевое слово record
                Expect("record", "после '='");

                // 5. Проверяем список полей
                bool hasFields = false;
                while (i < tokens.Count && (tokens[i].Type == TokenType.Identifier || tokens[i].Value == ","))
                {
                    if (tokens[i].Value == ",")
                    {
                        i++;
                        if (!CheckIdentifier("после ','")) break;
                    }
                    else if (tokens[i].Type == TokenType.Identifier)
                    {
                        hasFields = true;
                        i++;
                    }
                }

                if (!hasFields)
                {
                    errors.Add("Ожидался хотя бы один идентификатор поля");
                }

                // 6. Проверяем двоеточие
                Expect(":", "после списка полей");

                // 7. Проверяем тип полей
                Expect("real", "в качестве типа поля");

                // 8. Проверяем закрывающее ключевое слово
                Expect("end", "в конце объявления");

                // 9. Проверяем точку с запятой (необязательную)
                if (i < tokens.Count && tokens[i].Value == ";") i++;
            }
            catch (Exception ex)
            {
                errors.Add($"Ошибка при разборе: {ex.Message}");
            }

            return errors;
        }

        private List<Token> Tokenize(string input)
        {
            var tokens = new List<Token>();
            var pattern = @"\w+|[:,;=]|\S";
            var matches = Regex.Matches(input, pattern);

            foreach (Match match in matches)
            {
                string val = match.Value;
                int pos = match.Index;

                if (Keywords.Contains(val))
                    tokens.Add(new Token(TokenType.Keyword, val, pos));
                else if (Regex.IsMatch(val, @"^[a-zA-Z_]\w*$"))
                    tokens.Add(new Token(TokenType.Identifier, val, pos));
                else if (Regex.IsMatch(val, @"^[:,;=]$"))
                    tokens.Add(new Token(TokenType.Symbol, val, pos));
            }

            return tokens;
        }
    }
}