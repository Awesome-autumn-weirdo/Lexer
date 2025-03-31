using System;
using System.Collections.Generic;

namespace Lexer
{
    public class RecordParser
    {
        private string input;
        private int position;
        private List<string> errors = new List<string>();

        public List<string> ParseRecord(string code)
        {
            input = code;
            position = 0;
            errors.Clear();

            try
            {
                SkipWhitespace();
                while (position < input.Length)
                {
                    ParseTypeDeclaration();
                    SkipWhitespace();
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Критическая ошибка: {ex.Message}");
            }

            return errors;
        }

        private void ParseTypeDeclaration()
        {
            if (!MatchKeyword("type"))
            {
                AddError("Ожидалось ключевое слово 'type'");
                SkipToKeyword(new string[] { "type" });
                return;
            }

            SkipWhitespace();
            ParseIdentifier();
            SkipWhitespace();

            if (!MatchChar('='))
            {
                AddError("Ожидалось '=' после идентификатора");
                SkipToKeyword(new string[] { "type" });
                return;
            }

            SkipWhitespace();

            // Если "record" отсутствует, пропускаем до следующего "type"
            if (!PeekKeyword("record"))
            {
                AddError("Ожидалось ключевое слово 'record'");
                SkipToKeyword(new string[] { "type" });
                return;
            }

            ParseRecord();
        }


        private void ParseRecord()
        {
            if (!MatchKeyword("record"))
            {
                AddError("Ожидалось ключевое слово 'record'");
                SkipToKeyword(new string[] { "end", "type" }); // Ищем следующий блок
                return;
            }

            SkipWhitespace();
            ParseFields();

            if (!MatchKeyword("end"))
            {
                AddError("Ожидалось ключевое слово 'end'");
                SkipToKeyword(new string[] { "type" }); // Ищем следующий тип
                return;
            }

            SkipWhitespace();

            if (!MatchChar(';'))
            {
                AddError("Ожидалось ';' после 'end'");
                SkipToKeyword(new string[] { "type" });
            }
        }

        private void SkipToKeyword(string[] keywords)
        {
            int startPos = position; // Запоминаем начальную позицию
            while (position < input.Length)
            {
                foreach (var keyword in keywords)
                {
                    if (PeekKeyword(keyword)) return;
                }
                position++;

                // Если прошло 50 символов и ничего не нашли — выходим, чтобы избежать зависания
                if (position - startPos > 50)
                {
                    AddError("Не удалось найти ожидаемое ключевое слово");
                    return;
                }
            }
        }


        private void ParseFields()
        {
            while (position < input.Length && !PeekKeyword("end"))
            {
                SkipWhitespace();
                if (PeekKeyword("end")) break;

                var identifiers = new List<string>();
                do
                {
                    SkipWhitespace();
                    if (position >= input.Length || !char.IsLetter(input[position]))
                    {
                        AddError("Ожидался идентификатор поля");
                        SkipToKeyword(new string[] { "end", ";" });
                        return;
                    }

                    int start = position;
                    ParseIdentifier();
                    identifiers.Add(input.Substring(start, position - start));

                    SkipWhitespace();
                }
                while (MatchChar(','));

                if (identifiers.Count > 0)
                {
                    SkipWhitespace();
                    if (!MatchChar(':'))
                    {
                        AddError("Ожидалось ':' после списка полей");
                        SkipToKeyword(new string[] { "end", ";" });
                        continue;
                    }

                    SkipWhitespace();
                    ParseType();
                    SkipWhitespace();

                    if (!MatchChar(';') && !PeekKeyword("end"))
                    {
                        AddError("Ожидалось ';' после типа поля");
                        SkipToKeyword(new string[] { "end", ";" });
                    }
                }
            }
        }


        private void ParseType()
        {
            string[] validTypes = { "integer", "real", "string", "boolean", "char" };
            bool typeFound = false;

            foreach (var type in validTypes)
            {
                if (MatchKeyword(type))
                {
                    typeFound = true;
                    break;
                }
            }

            if (!typeFound)
            {
                AddError($"Недопустимый тип данных. Ожидалось: {string.Join(", ", validTypes)}");
                SkipToNextField();
            }
        }

        private void ParseIdentifier()
        {
            if (position >= input.Length || !char.IsLetter(input[position]))
            {
                AddError("Ожидался идентификатор");
                return;
            }

            while (position < input.Length && (char.IsLetterOrDigit(input[position]) || input[position] == '_'))
            {
                position++;
            }
        }

        private void SkipToNextField()
        {
            while (position < input.Length && input[position] != ';' && !PeekKeyword("end"))
            {
                position++;
            }
        }

        private void SkipToNextType()
        {
            while (position < input.Length)
            {
                if (PeekKeyword("type")) return;
                position++;
            }
        }


        private bool PeekKeyword(string keyword)
        {
            if (position + keyword.Length > input.Length)
                return false;

            for (int i = 0; i < keyword.Length; i++)
            {
                if (char.ToLower(input[position + i]) != char.ToLower(keyword[i]))
                    return false;
            }

            // Убедимся, что после ключевого слова идет разделитель
            if (position + keyword.Length < input.Length)
            {
                char nextChar = input[position + keyword.Length];
                if (char.IsLetterOrDigit(nextChar) || nextChar == '_')
                    return false;
            }

            return true;
        }

        private bool MatchKeyword(string keyword)
        {
            if (PeekKeyword(keyword))
            {
                position += keyword.Length;
                return true;
            }
            return false;
        }

        private bool MatchChar(char c)
        {
            if (position < input.Length && input[position] == c)
            {
                position++;
                return true;
            }
            return false;
        }

        private void SkipWhitespace()
        {
            while (position < input.Length && char.IsWhiteSpace(input[position]))
            {
                position++;
            }
        }

        private void AddError(string message)
        {
            errors.Add(message);
        }

    }
}