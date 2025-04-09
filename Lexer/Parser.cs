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

            // Восстанавливающее ожидание ключевого слова/символа
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
                }

                i++; // Всегда двигаем, даже если неправильное
            }

            // Восстанавливающее ожидание идентификатора
            bool CheckIdentifier(string context)
            {
                if (i >= tokens.Count)
                {
                    errors.Add($"Ожидался идентификатор {context}, найдено 'EOF'");
                    return false;
                }

                if (tokens[i].Type != TokenType.Identifier)
                {
                    errors.Add($"Ожидался идентификатор {context}, найдено '{tokens[i].Value}'");
                    i++; // Пропускаем неправильный токен
                    return false;
                }

                i++; // Пропускаем корректный токен
                return true;
            }

            // Парсинг структуры записи
            try
            {
                Expect("type", "в начале объявления");
                CheckIdentifier("после 'type'");
                Expect("=", "после имени типа");
                Expect("record", "после '='");

                // Разбор списка полей
                bool hasFields = false;
                while (i < tokens.Count && (tokens[i].Type == TokenType.Identifier || tokens[i].Value == ","))
                {
                    if (tokens[i].Value == ",")
                    {
                        i++;
                        CheckIdentifier("после ','");
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

                Expect(":", "после списка полей");
                Expect("real", "в качестве типа поля");
                Expect("end", "в конце объявления");

                // Проверяем точку с запятой
                if (i < tokens.Count)
                {
                    if (tokens[i].Value == ";")
                    {
                        i++;
                    }
                    else
                    {
                        errors.Add($"Ожидалась ';' после 'end', найдено '{tokens[i].Value}'");
                        i++;
                    }
                }
                else
                {
                    errors.Add("Ожидалась ';' после 'end', найдено 'EOF'");
                }
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
