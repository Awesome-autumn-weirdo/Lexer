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
                SkipWhitespace();
                if (position >= text.Length) break;

                switch (state)
                {
                    case 1: State1(); break;
                    case 2: State2(); break;
                    case 3: State3(); break;
                    case 4: State4(); break;
                    case 5: State5(); break;
                    case 6: State6(); break;
                    case 7: State7(); break;
                }
            }

            if (state != 100)
            {
                HandleError("Неожиданное завершение конструкции", "", position);
            }

            foreach (var error in errors)
            {
                errorsDataGridView.Rows.Add(-1, "Ошибка", error.Message, error.LineNumber, $"Позиция: {error.Position}");
                HighlightError(editorRichTextBox, error.Position, 1);
            }

            return errors.Count == 0;
        }

        private void State1()
        {
            if (!MatchKeyword("type"))
            {
                HandleError("Ожидается ключевое слово 'type'", "", position);
                SkipToKeyword(new[] { "type" });
                return;
            }

            if (!RequireWhitespace())
            {
                HandleError("После 'type' должен быть пробел", "", position);
                SkipToKeyword(new[] { "=" });
                return;
            }

            state = 2;
        }

        private void State2()
        {
            if (!ParseIdentifier(out currentId))
            {
                HandleError("Ожидается идентификатор", "", position);
                SkipToKeyword(new[] { "=" });
                return;
            }

            if (!RequireWhitespace())
            {
                HandleError("После идентификатора должен быть пробел", "", position);
                SkipToKeyword(new[] { "record" });
                return;
            }

            state = 3;
        }

        private void State3()
        {
            if (!MatchChar('='))
            {
                HandleError("Ожидается '='", "", position);
                SkipToKeyword(new[] { "record" });
                return;
            }

            if (!RequireWhitespace())
            {
                HandleError("После '=' должен быть пробел", "", position);
                SkipToKeyword(new[] { "record" });
                return;
            }

            state = 4;
        }

        private void State4()
        {
            if (!MatchKeyword("record"))
            {
                HandleError("Ожидается ключевое слово 'record'", "", position);
                SkipToKeyword(new[] { "end" });
                return;
            }

            if (!RequireWhitespace())
            {
                HandleError("После 'record' должен быть пробел", "", position);
            }

            state = 5;
        }

        private void State5()
        {
            // Обработка списка полей через запятую
            bool hasFields = false;

            do
            {
                SkipWhitespace();
                if (!ParseIdentifier(out _))
                {
                    if (!hasFields)
                    {
                        HandleError("Ожидается имя поля", "", position);
                        SkipToKeyword(new[] { ":", "end" });
                    }
                    break;
                }
                hasFields = true;

                SkipWhitespace();
                if (MatchChar(','))
                {
                    continue; // Продолжаем собирать поля
                }
                else if (MatchChar(':'))
                {
                    state = 6;
                    return;
                }
                else
                {
                    HandleError("Ожидается ',' или ':' после имени поля", "", position);
                    SkipToKeyword(new[] { ":", "end" });
                    break;
                }
            } while (position < text.Length);

            if (state == 5) // Если не нашли ':'
            {
                HandleError("Ожидается ':' после списка полей", "", position);
                SkipToKeyword(new[] { "end" });
            }
        }

        private void State6()
        {
            SkipWhitespace();
            if (!ParseType(out _))
            {
                HandleError("Ожидается тип данных", "", position);
                SkipToKeyword(new[] { "end" });
                return;
            }

            SkipWhitespace();
            if (MatchChar(';'))
            {
                state = 7;
            }
            else
            {
                // После типа данных не обязательно ';'
                state = 7; // Переход в следующее состояние без ;
            }
        }

        private void State7()
        {
            SkipWhitespace();
            if (MatchKeyword("end"))
            {
                SkipWhitespace();
                if (MatchChar(';'))
                {
                    state = 100; // Успешное завершение
                }
                else
                {
                    HandleError("Ожидается ';' после 'end'", "", position);
                }
            }
            else
            {
                // Возвращаемся к обработке полей
                state = 5;
            }
        }

        private bool MatchKeyword(string keyword)
        {
            if (position + keyword.Length > text.Length)
                return false;

            for (int i = 0; i < keyword.Length; i++)
            {
                if (char.ToLower(text[position + i]) != char.ToLower(keyword[i]))
                    return false;
            }

            if (position + keyword.Length < text.Length &&
                (char.IsLetterOrDigit(text[position + keyword.Length]) || text[position + keyword.Length] == '_'))
                return false;

            position += keyword.Length;
            return true;
        }

        private bool MatchChar(char c)
        {
            if (position < text.Length && text[position] == c)
            {
                position++;
                return true;
            }
            return false;
        }

        private bool RequireWhitespace()
        {
            if (position < text.Length && char.IsWhiteSpace(text[position]))
            {
                SkipWhitespace();
                return true;
            }
            return false;
        }

        private void SkipWhitespace()
        {
            while (position < text.Length && char.IsWhiteSpace(text[position]))
            {
                if (text[position] == '\n')
                {
                    currentLine++;
                    currentLinePosition = 0;
                }
                position++;
            }
        }

        private bool ParseIdentifier(out string identifier)
        {
            identifier = "";
            if (position >= text.Length || !char.IsLetter(text[position]))
                return false;

            int start = position;
            while (position < text.Length && (char.IsLetterOrDigit(text[position]) || text[position] == '_'))
            {
                position++;
            }

            identifier = text.Substring(start, position - start);
            return true;
        }

        private bool ParseType(out string type)
        {
            type = "";
            string[] validTypes = { "real", "integer", "string", "char", "boolean" };
            foreach (var t in validTypes)
            {
                if (MatchKeyword(t))
                {
                    type = t;
                    return true;
                }
            }
            return false;
        }

        private void SkipToKeyword(string[] keywords)
        {
            while (position < text.Length)
            {
                foreach (var keyword in keywords)
                {
                    if (MatchKeyword(keyword)) return;
                }
                position++;
            }
        }

        private void HandleError(string message, string incorrectFragment, int pos)
        {
            errors.Add(new ParseError(message, incorrectFragment, currentLine, pos));
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
