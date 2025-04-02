using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Lexer
{
    public class Parser
    {
        private string currentId;
        private int state;
        private string text;
        private int position;
        private List<ParseError> errors;
        private int currentLine;
        private int currentLinePosition;

        public List<ParseError> GetErrors()
        {
            return errors;
        }

        public bool Parse(string inputText, DataGridView errorsDataGridView, RichTextBox editorRichTextBox)
        {
            text = inputText;
            position = 0;
            currentLine = 1;
            currentLinePosition = 0;
            state = 1;
            currentId = "";
            errors = new List<ParseError>();

            while (position < text.Length && state != 100) // 100 - конечное состояние
            {
                char currentChar = text[position];

                // Обработка перевода строки
                if (currentChar == '\n')
                {
                    currentLine++;
                    currentLinePosition = 0;
                    position++;
                    continue;
                }

                // Пропускаем пробелы и табы (кроме тех, что являются разделителями в грамматике)
                if (currentChar == ' ' || currentChar == '\t')
                {
                    position++;
                    currentLinePosition++;
                    continue;
                }

                switch (state)
                {
                    case 1: State1(currentChar); break;
                    case 2: State2(currentChar); break;
                    case 3: State3(currentChar); break;
                    case 4: State4(currentChar); break;
                    case 5: State5(currentChar); break;
                    case 6: State6(currentChar); break;
                    case 7: State7(currentChar); break;
                    case 8: State8(currentChar); break;
                    case 9: State9(currentChar); break;
                    case 10: State10(currentChar); break;
                }

                position++;
                currentLinePosition++;
            }

            // Проверяем, достигли ли мы конечного состояния
            if (state != 100)
            {
                HandleError("Неожиданное завершение конструкции", "", position);
            }

            // Выводим ошибки в DataGridView
            foreach (var error in errors)
            {
                errorsDataGridView.Rows.Add(-1, "Ошибка", error.Message, error.LineNumber, $"Позиция: {error.Position}");
                HighlightError(editorRichTextBox, error.Position, 1);
            }

            return errors.Count == 0;
        }

        private void State1(char c)
        {
            // Ожидаем ключевое слово 'type'
            if (c == 't' && position + 3 < text.Length && text.Substring(position, 4) == "type")
            {
                position += 3; // Пропускаем оставшиеся символы 'ype'
                currentLinePosition += 3;
                state = 2;
            }
            else
            {
                HandleError("Ожидается ключевое слово 'type'", c.ToString(), position);
            }
        }

        private void State2(char c)
        {
            // Ожидаем хотя бы один пробел после 'type'
            if (c == ' ')
            {
                // Пропускаем все пробелы и табуляции
                while (position < text.Length && (text[position] == ' ' || text[position] == '\t'))
                {
                    position++;
                    currentLinePosition++;
                }

                state = 3; // Переход к идентификатору
            }
            else
            {
                HandleError("После 'type' должен быть хотя бы один пробел", c.ToString(), position);
            }
        }

        private void State3(char c)
        {
            // Начало идентификатора (буква)
            if (IsLetter(c))
            {
                currentId = c.ToString();
                state = 4;
            }
            else
            {
                HandleError("Ожидается идентификатор (начинается с буквы)", c.ToString(), position);
            }
        }

        private void State4(char c)
        {
            // Продолжение идентификатора (буквы/цифры) или '='
            if (IsLetter(c) || IsDigit(c))
            {
                currentId += c;
            }
            else if (c == '=')
            {
                state = 5;
            }
            else
            {
                HandleError("Ожидается продолжение идентификатора или знак '='", c.ToString(), position);
            }
        }

        private void State5(char c)
        {
            // Ожидаем ключевое слово 'record'
            if (c == 'r' && position + 5 < text.Length && text.Substring(position, 6) == "record")
            {
                position += 5; // Пропускаем оставшиеся символы 'ecord'
                currentLinePosition += 5;
                state = 6;
            }
            else
            {
                HandleError("Ожидается ключевое слово 'record'", c.ToString(), position);
            }
        }

        private void State6(char c)
        {
            // Ожидаем хотя бы один пробел после 'record'
            if (c == ' ')
            {
                // Пропускаем все пробелы и табуляции
                while (position < text.Length && (text[position] == ' ' || text[position] == '\t'))
                {
                    position++;
                    currentLinePosition++;
                }

                state = 7; // Переход к имени поля
            }
            else
            {
                HandleError("После 'record' должен быть хотя бы один пробел", c.ToString(), position);
            }
        }

        private void State7(char c)
        {
            // Начало имени поля (буква)
            if (IsLetter(c))
            {
                state = 8;
            }
            else
            {
                HandleError("Ожидается имя поля (начинается с буквы)", c.ToString(), position);
            }
        }

        private void State8(char c)
        {
            // Продолжение имени поля или разделители
            if (IsLetter(c) || IsDigit(c))
            {
                // Продолжаем собирать имя поля
            }
            else if (c == ',')
            {
                state = 7; // Ожидаем следующее поле
            }
            else if (c == ':')
            {
                state = 9; // Ожидаем тип поля
            }
            else
            {
                HandleError("Ожидается продолжение имени поля, запятая или двоеточие", c.ToString(), position);
            }
        }

        private void State9(char c)
        {
            // Ожидаем тип поля (real, integer, string и т.д.)
            if (IsLetter(c))
            {
                // Проверяем весь тип
                int start = position;
                while (position < text.Length && IsLetter(text[position]))
                {
                    position++;
                    currentLinePosition++;
                }

                string type = text.Substring(start, position - start);
                position--; // Корректируем позицию, так как основной цикл тоже увеличит position
                currentLinePosition--;

                if (!IsValidType(type))
                {
                    HandleError($"Неверный тип поля: {type}", type, start);
                }

                state = 10; // Ожидаем 'end' или новое поле
            }
            else
            {
                HandleError("Ожидается тип поля (real, integer, string и т.д.)", c.ToString(), position);
            }
        }

        private void State10(char c)
        {
            // Ожидаем 'end' или новое поле
            if (c == 'e' && position + 2 < text.Length && text.Substring(position, 3) == "end")
            {
                position += 2; // Пропускаем 'nd'
                currentLinePosition += 2;
                state = 11; // Ожидаем ';'
            }
            else if (c == ',')
            {
                state = 7; // Новое поле
            }
            else
            {
                HandleError("Ожидается 'end' или запятая для нового поля", c.ToString(), position);
            }
        }

        private void State11(char c)
        {
            // Ожидаем ';'
            if (c == ';')
            {
                state = 100; // Успешное завершение
            }
            else
            {
                HandleError("Ожидается ';' после 'end'", c.ToString(), position);
            }
        }

        private bool IsLetter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        private bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        private bool IsValidType(string type)
        {
            string[] validTypes = { "real", "integer", "string", "char", "boolean" };
            return Array.IndexOf(validTypes, type) >= 0;
        }

        private void HandleError(string message, string incorrectFragment, int position)
        {
            errors.Add(new ParseError(message, incorrectFragment, currentLine, position));
        }

        private void HighlightError(RichTextBox richTextBox, int start, int length)
        {
            richTextBox.SelectionStart = start;
            richTextBox.SelectionLength = length;
            richTextBox.SelectionBackColor = Color.Pink;
            richTextBox.SelectionColor = Color.Black;
        }
    }

    public class ParseError
    {
        public string Message { get; }
        public string IncorrectFragment { get; }
        public int LineNumber { get; }
        public int Position { get; }

        public ParseError(string message, string incorrectFragment, int lineNumber, int position)
        {
            Message = message;
            IncorrectFragment = incorrectFragment;
            LineNumber = lineNumber;
            Position = position;
        }
    }
}