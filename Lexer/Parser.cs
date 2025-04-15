using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Lexer
{
    public class RecordParser
    {
        private static readonly HashSet<string> Keywords = new HashSet<string> { "type", "record", "end", "real", "integer", "string", "boolean", "char" };

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

            public override string ToString() => $"{Type}: {Value} (at {Position})";
        }

        public List<string> ParseRecord(string input, RichTextBox richTextBox)
        {
            var tokens = Tokenize(input, richTextBox);
            var errors = new List<string>();
            int i = 0;

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

                i++;
            }

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
                    i++;
                    return false;
                }

                i++;
                return true;
            }

            try
            {
                Expect("type", "в начале объявления");
                CheckIdentifier("после 'type'");

                // Проверка на '=' с восстановлением
                if (i >= tokens.Count || tokens[i].Value != "=")
                {
                    errors.Add($"Ожидалось '=' после имени типа, найдено '{(i < tokens.Count ? tokens[i].Value : "EOF")}'");

                    // Вставим виртуальный '='
                    int pos = i < tokens.Count ? tokens[i].Position : 0;
                    tokens.Insert(i, new Token(TokenType.Symbol, "=", pos));
                }
                i++;

                // Проверка на 'record' с восстановлением
                if (i >= tokens.Count || tokens[i].Value != "record")
                {
                    errors.Add($"Ожидалось 'record' после '=', найдено '{(i < tokens.Count ? tokens[i].Value : "EOF")}'");

                    int pos = i < tokens.Count ? tokens[i].Position : 0;
                    tokens.Insert(i, new Token(TokenType.Keyword, "record", pos));
                }
                i++;


                while (i < tokens.Count && tokens[i].Value != "end")
                {
                    var fieldNames = new List<string>();

                    // Первая переменная
                    if (!CheckIdentifier("в списке полей"))
                        break;
                    fieldNames.Add(tokens[i - 1].Value);

                    // Остальные идентификаторы (с запятыми или без — восстанавливаем)
                    while (i < tokens.Count)
                    {
                        if (tokens[i].Value == ",")
                        {
                            i++;
                            if (!CheckIdentifier("после ',' в списке полей")) break;
                            fieldNames.Add(tokens[i - 1].Value);
                        }
                        else if (tokens[i].Type == TokenType.Identifier)
                        {
                            // Вставка "виртуальной запятой"
                            errors.Add($"Ожидалась ',' между идентификаторами, найдено '{tokens[i].Value}'");
                            if (!CheckIdentifier("в списке полей после восстановления")) break;
                            fieldNames.Add(tokens[i - 1].Value);
                        }
                        else break;
                    }

                    // Двоеточие
                    if (i >= tokens.Count || tokens[i].Value != ":")
                    {
                        errors.Add($"Ожидалось ':' после списка полей, найдено '{(i < tokens.Count ? tokens[i].Value : "EOF")}'");
                        int pos = i < tokens.Count ? tokens[i].Position : 0;
                        tokens.Insert(i, new Token(TokenType.Symbol, ":", pos));
                    }
                    i++;

                    // Тип поля
                    if (i >= tokens.Count || tokens[i].Type != TokenType.Keyword || !Keywords.Contains(tokens[i].Value))
                    {
                        errors.Add($"Ожидался тип поля, найдено '{(i < tokens.Count ? tokens[i].Value : "EOF")}'");

                        int pos = i < tokens.Count ? tokens[i].Position : 0;
                        tokens.Insert(i, new Token(TokenType.Keyword, "real", pos));
                        i++;
                    }
                    i++;

                    // ; или end
                    if (i < tokens.Count && tokens[i].Value == ";")
                    {
                        i++;
                    }
                    else if (i < tokens.Count && tokens[i].Value != "end")
                    {
                        //errors.Add($"Ожидалась ; или end, найдено '{tokens[i].Value}'");

                        // Вставим виртуальный токен ';' — чтобы parser не сломался
                        tokens.Insert(i, new Token(TokenType.Symbol, ";", tokens[i].Position));
                        i++;
                    }
                    else
                    {
                        // Возможно конец record-а без ; — ничего не делаем
                    }

                }

                // Завершение record
                if (i >= tokens.Count || tokens[i].Value != "end")
                {
                    errors.Add($"Ожидалось 'end' в конце объявления, найдено '{(i < tokens.Count ? tokens[i].Value : "EOF")}'");
                    int pos = i < tokens.Count ? tokens[i].Position : 0;
                    tokens.Insert(i, new Token(TokenType.Keyword, "end", pos));
                }
                // В любом случае — теперь обрабатываем end
                if (i < tokens.Count && tokens[i].Value == "end")
                {
                    i++;
                }

                // Завершающая точка с запятой
                if (i < tokens.Count && tokens[i].Value == ";")
                {
                    i++;
                }
                else
                {
                    errors.Add($"Ожидалась ';' после 'end', найдено '{(i < tokens.Count ? tokens[i].Value : "EOF")}'");
                }

            }
            catch (Exception ex)
            {
                errors.Add($"Ошибка при разборе: {ex.Message}");
            }

            return errors;
        }
        private List<Token> Tokenize(string input, RichTextBox richTextBox)
        {
            var tokens = new List<Token>();
            var lexErrors = new List<(int start, int length)>(); // Список недопустимых символов

            // Оставляем только допустимые символы, убираем все недопустимые (кроме пробела и переноса строки)
            var cleanedInput = Regex.Replace(input, @"[^a-zA-Z0-9_:\s,;=\n]", m =>
            {
                // Если символ недопустимый, добавляем его в список ошибок
                lexErrors.Add((m.Index, 1));
                return ""; // Убираем недопустимые символы
            });

            var pattern = @"\w+|[:,;=]|\S"; // Регулярное выражение для извлечения токенов
            var matches = Regex.Matches(cleanedInput, pattern);

            string currentToken = ""; // Строка для склеивания токенов

            foreach (Match match in matches)
            {
                string val = match.Value;
                int pos = match.Index;

                // Если токен состоит из буквенно-цифровых символов или подчеркивания (идентификатор или ключевое слово)
                if (Regex.IsMatch(val, @"^[a-zA-Z_]\w*$"))
                {
                    if (currentToken != "")
                    {
                        // Завершаем предыдущий токен
                        if (Keywords.Contains(currentToken))
                            tokens.Add(new Token(TokenType.Keyword, currentToken, pos));
                        else
                            tokens.Add(new Token(TokenType.Identifier, currentToken, pos));

                        currentToken = ""; // Сбрасываем текущий токен
                    }

                    // Если текущий токен совпадает с ключевым словом, добавляем его
                    if (Keywords.Contains(val))
                    {
                        tokens.Add(new Token(TokenType.Keyword, val, pos));
                    }
                    else
                    {
                        // Иначе собираем как идентификатор
                        currentToken += val;
                    }
                }
                // Если встречается символ, например, '=', ':', ',', ';'
                else if (Regex.IsMatch(val, @"^[:,;=]$"))
                {
                    // Если текущий собранный токен есть, добавляем его в список
                    if (!string.IsNullOrEmpty(currentToken))
                    {
                        if (Keywords.Contains(currentToken))
                            tokens.Add(new Token(TokenType.Keyword, currentToken, input.Length - currentToken.Length));
                        else
                            tokens.Add(new Token(TokenType.Identifier, currentToken, input.Length - currentToken.Length));
                        currentToken = ""; // Сбрасываем текущий токен
                    }

                    // Добавляем текущий символ как токен
                    tokens.Add(new Token(TokenType.Symbol, val, pos));
                }
                // Пропускаем пробелы и переносы строк (они не добавляются в список токенов)
            }

            // Если после обработки остался незавершенный токен
            if (!string.IsNullOrEmpty(currentToken))
            {
                if (Keywords.Contains(currentToken))
                    tokens.Add(new Token(TokenType.Keyword, currentToken, input.Length - currentToken.Length));
                else
                    tokens.Add(new Token(TokenType.Identifier, currentToken, input.Length - currentToken.Length));
            }

            // Подсвечиваем ошибки
            foreach (var error in lexErrors)
            {
                HighlightError(richTextBox, error.start, error.length);
            }

            return tokens;
        }

        private void HighlightError(RichTextBox richTextBox, int start, int length)
        {
            int originalSelectionStart = richTextBox.SelectionStart;
            int originalSelectionLength = richTextBox.SelectionLength;

            richTextBox.Select(start, length);
            richTextBox.SelectionBackColor = Color.Plum;

            // Возвращаем выделение обратно, чтобы не сбивать курсор
            richTextBox.Select(originalSelectionStart, originalSelectionLength);
        }

    }
}
