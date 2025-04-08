using System.Windows.Forms;
using System;
using System.Drawing;

namespace Lexer
{
    public class Scanner
    {
        private string[] keywords = { "type", "record", "end", "integer", "real", "char", "boolean", "string", "var" };
        private string[] operators = { "=", ":", ",", ";" };
        private string[] separators = { " ", "\t", "\n", "\r" };

        public bool Analyze(string text, DataGridView errorsDataGridView, RichTextBox editorRichTextBox)
        {
            errorsDataGridView.Rows.Clear();

            int lineNumber = 1;
            int positionInLine = 0;
            int globalPosition = 0;
            bool lastWasKeyword = false;
            bool isSuccess = true;

            while (globalPosition < text.Length)
            {
                char currentChar = text[globalPosition];

                switch (currentChar)
                {
                    case '\n':
                        lineNumber++;
                        positionInLine = 0;
                        globalPosition++;
                        continue;

                    case ' ':
                    case '\t':
                        int spaceStart = positionInLine;
                        while (globalPosition < text.Length && (text[globalPosition] == ' ' || text[globalPosition] == '\t'))
                        {
                            globalPosition++;
                            positionInLine++;
                        }

                        if (lastWasKeyword)
                        {
                            AddTokenToDataGridView(errorsDataGridView, " ", "(пробел)", lineNumber, spaceStart, positionInLine);
                            lastWasKeyword = false;
                        }
                        continue;

                    default:

                        if (IsOperator(currentChar))
                        {
                            string token = currentChar.ToString();
                            int endPosition = positionInLine + 1;
                            AddTokenToDataGridView(errorsDataGridView, token, "Оператор", lineNumber, positionInLine, endPosition);
                            globalPosition++;
                            positionInLine++;
                            lastWasKeyword = false;
                            continue;
                        }

                        if (IsLatinLetter(currentChar))
                        {
                            int end = globalPosition;
                            while (end < text.Length &&
                                   (IsLatinLetter(text[end]) || char.IsDigit(text[end]) || text[end] == '_'))
                            {
                                end++;
                            }

                            string token = text.Substring(globalPosition, end - globalPosition);
                            string tokenType = IsKeyword(token) ? "Ключевое слово" : "Идентификатор";
                            int endPosition = positionInLine + token.Length;

                            AddTokenToDataGridView(errorsDataGridView, token, tokenType, lineNumber, positionInLine, endPosition);

                            globalPosition = end;
                            positionInLine = endPosition;
                            lastWasKeyword = tokenType == "Ключевое слово";
                            continue;
                        }

                        if (char.IsDigit(currentChar))
                        {
                            int end = globalPosition;
                            bool isReal = false;

                            while (end < text.Length && (char.IsDigit(text[end]) || text[end] == '.' || text[end] == ','))
                            {
                                if (text[end] == '.' || text[end] == ',')
                                {
                                    if (isReal || end + 1 >= text.Length || !char.IsDigit(text[end + 1]))
                                        break;
                                    isReal = true;
                                }
                                end++;
                            }

                            string token = text.Substring(globalPosition, end - globalPosition);
                            int endPosition = positionInLine + token.Length;
                            string tokenType = isReal ? "Вещественное число" : "Целое без знака";

                            AddTokenToDataGridView(errorsDataGridView, token, tokenType, lineNumber, positionInLine, endPosition);

                            globalPosition = end;
                            positionInLine = endPosition;
                            lastWasKeyword = false;
                            continue;
                        }

                        // Обработка цепочки недопустимых символов (в т.ч. русских)
                        int errorStartGlobal = globalPosition;
                        int errorStartLine = positionInLine;

                        while (globalPosition < text.Length &&
                               !IsLatinLetter(text[globalPosition]) &&
                               !char.IsDigit(text[globalPosition]) &&
                               !IsOperator(text[globalPosition]) &&
                               !char.IsWhiteSpace(text[globalPosition]))
                        {
                            globalPosition++;
                            positionInLine++;
                        }

                        AddTokenToDataGridView(errorsDataGridView, text[errorStartGlobal].ToString(), "Недопустимый символ", lineNumber, errorStartLine, errorStartLine + 1);
                        HighlightError(editorRichTextBox, errorStartGlobal, 1);
                        isSuccess = false;
                        continue;
                }
            }

            return isSuccess;
        }
        private bool IsLatinLetter(char ch)
        {
            return (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z');
        }

        private bool IsOperator(char ch) => Array.Exists(operators, op => op[0] == ch);

        private bool IsKeyword(string token) => Array.Exists(keywords, kw => kw == token);

        private void AddTokenToDataGridView(DataGridView dataGridView, string token, string tokenType, int lineNumber, int startPos, int endPos)
        {
            string positionRange = $"с {startPos} по {endPos - 1} символ";
            dataGridView.Rows.Add(GetTokenCode(token, tokenType), tokenType, token, lineNumber, positionRange);
        }

        private int GetTokenCode(string token, string tokenType)
        {
            switch (tokenType)
            {
                case "Ключевое слово": return Array.IndexOf(keywords, token) + 1;
                case "Идентификатор": return 10;
                case "(пробел)": return 11;
                case "Оператор": return Array.IndexOf(operators, token) + 12;
                case "Целое без знака": return 16;
                case "Вещественное число": return 17;
                case "Недопустимый символ": return -1;
                default: return 0;
            }
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
