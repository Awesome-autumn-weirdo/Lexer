using System;
using System.Collections.Generic;
using System.Linq;

namespace Lexer
{
    class ParseError : Exception
    {
        public int Idx { get; }
        public string IncorrStr { get; }

        public ParseError(string msg, string rem, int index) : base(msg)
        {
            IncorrStr = rem;
            Idx = index;
        }
    }

    class Character
    {
        public char Char { get; }
        public int Idx { get; }
        public int Line { get; }
        public int Position { get; }

        public Character(char c, int idx, int line, int position)
        {
            Char = c;
            Idx = idx;
            Line = line;
            Position = position;
        }
    }

    class CharChain
    {
        private readonly string[] lines;
        private int currentLine = 0;
        private int currentPos = 1;
        private int globalIndex = 0;

        public CharChain(string text)
        {
            lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        }

        public Character GetNext()
        {
            if (currentLine >= lines.Length)
                return new Character('\0', globalIndex, currentLine + 1, 1);

            string line = lines[currentLine];
            if (currentPos > line.Length)
            {
                currentLine++;
                currentPos = 1;
                return GetNext();
            }

            char c = line[currentPos - 1];
            var character = new Character(c, globalIndex, currentLine + 1, currentPos);

            currentPos++;
            globalIndex++;
            return character;
        }

        public Character Next()
        {
            if (currentLine >= lines.Length)
                return new Character('\0', globalIndex, currentLine + 1, 1);

            string line = lines[currentLine];
            if (currentPos > line.Length)
            {
                return new Character('\n', globalIndex, currentLine + 1, currentPos);
            }

            return new Character(line[currentPos - 1], globalIndex, currentLine + 1, currentPos);
        }

        public void SkipSpaces()
        {
            while (true)
            {
                if (currentLine >= lines.Length)
                    break;

                string line = lines[currentLine];
                while (currentPos <= line.Length && char.IsWhiteSpace(line[currentPos - 1]))
                {
                    currentPos++;
                    globalIndex++;
                }

                if (currentPos > line.Length)
                {
                    currentLine++;
                    currentPos = 1;
                }
                else
                {
                    break;
                }
            }
        }

        public (int line, int position) GetLineAndPosition(int charIndex)
        {
            int line = 1;
            int accumulatedLength = 0;

            foreach (string l in lines)
            {
                if (charIndex < accumulatedLength + l.Length + 1)
                {
                    int position = charIndex - accumulatedLength + 1;
                    return (line, Math.Min(position, l.Length + 1));
                }
                accumulatedLength += l.Length + 1;
                line++;
            }

            return (line, 1);
        }
    }

    class RecordTypeParser
    {
        private CharChain chain;
        private int state;
        private readonly List<ParseError> errors;
        private int iterationCount;
        private const int MaxIterations = 10000;

        public RecordTypeParser()
        {
            errors = new List<ParseError>();
        }

        public List<ParseError> GetErrors() => errors;

        public bool Parse(CharChain c)
        {
            chain = c;
            state = 1;
            iterationCount = 0;
            errors.Clear();

            while (iterationCount < MaxIterations)
            {
                iterationCount++;

                switch (state)
                {
                    case 1: state1(); break;
                    case 2: state2(); break;
                    case 3: state3(); break;
                    case 4: state4(); break;
                    case 5: state5(); break;
                    case 6: state6(); break;
                    case 7: state7(); break;
                    case 8: state8(); break;
                    case 9: state9(); break;
                    case 10: state10(); break;
                    case 11: state11(); break;
                    case 12: state12(); break;
                    case 13: state13(); break;
                    case 14: return errors.Count == 0;
                }

                chain.SkipSpaces();

                if (chain.Next().Char == '\0')
                {
                    if (state != 14 && state != 13 && state != 1)
                    {
                        errors.Add(new ParseError("Неожиданный конец файла", "", chain.Next().Idx));
                    }
                    return errors.Count == 0;
                }
            }

            errors.Add(new ParseError("Превышено максимальное количество итераций", "", chain.Next().Idx));
            return false;
        }

        private void handleError(string msg, string removed, Character c)
        {
            errors.Add(new ParseError(msg, removed, c.Idx));
        }

        private readonly HashSet<string> validTypes = new HashSet<string>
        {
            "integer", "real", "char", "boolean", "string", "var"
        };

        private void state1()
        {
            Character c = chain.Next();
            if (c.Char == 't')
            {
                chain.GetNext();
                state = 2;
            }
            else if (c.Char == '\0')
            {
                state = 14;
            }
            else
            {
                handleError("Ожидалось 'type' или конец файла", c.Char.ToString(), c);
                chain.GetNext();
            }
        }

        private void state2()
        {
            string expected = "ype";
            string actual = "";
            Character c;

            for (int i = 0; i < expected.Length; i++)
            {
                c = chain.GetNext();
                actual += c.Char;
                if (c.Char != expected[i])
                {
                    handleError($"Ожидалось 'type'. Найдено 't{actual}'", "t" + actual, c);
                    state = 3;
                    return;
                }
            }

            state = 3;
        }

        private void state3()
        {
            Character c = chain.GetNext();
            if (char.IsLetter(c.Char))
                state = 4;
            else
                handleError("Ожидался идентификатор", c.Char.ToString(), c);
        }

        private void state4()
        {
            Character c = chain.GetNext();
            if (char.IsLetterOrDigit(c.Char))
                state = 4;
            else if (c.Char == '=')
                state = 5;
            else
                handleError("Ожидался символ '='", c.Char.ToString(), c);
        }

        private void state5()
        {
            Character c = chain.GetNext();
            if (c.Char == 'r')
                state = 6;
            else
                handleError("Ожидалось 'record'", c.Char.ToString(), c);
        }

        private void state6()
        {
            string expected = "ecord";
            string actual = "";
            Character c;

            for (int i = 0; i < expected.Length; i++)
            {
                c = chain.GetNext();
                actual += c.Char;
                if (c.Char != expected[i])
                {
                    handleError($"Ожидалось 'record'. Найдено 'r{actual}'", "r" + actual, c);
                    state = 7;
                    return;
                }
            }

            state = 7;
            chain.SkipSpaces();
        }

        private void state7()
        {
            Character c = chain.GetNext();
            if (char.IsLetter(c.Char))
                state = 8;
            else
                handleError("Ожидался идентификатор поля", c.Char.ToString(), c);
        }

        private void state8()
        {
            Character c = chain.Next();

            if (char.IsLetterOrDigit(c.Char))
            {
                chain.GetNext();
                state = 8;
            }
            else if (c.Char == ',')
            {
                chain.GetNext();
                state = 7;
            }
            else if (c.Char == ':')
            {
                chain.GetNext();
                state = 9;
            }
            else if (char.IsWhiteSpace(c.Char))
            {
                // Пропускаем пробелы и проверяем следующий символ
                chain.SkipSpaces();
                c = chain.Next();

                if (char.IsLetter(c.Char) && validTypes.Contains(c.Char.ToString()))
                {
                    handleError("Пропущено двоеточие перед типом данных", "", c);
                    state = 9;
                }
                else
                {
                    handleError("Ожидалась ',' или ':'", "", c);
                    state = 7;
                }
            }
            else
            {
                handleError("Ожидалась ',' или ':' или продолжение идентификатора", c.Char.ToString(), c);
                chain.GetNext();
                state = 7;
            }
        }

        private void state9()
        {
            Character c = chain.Next();
            if (char.IsLetter(c.Char))
                state = 10;
            else
                handleError("Ожидался тип данных", c.Char.ToString(), c);
        }

        private void state10()
        {
            string typeName = "";
            Character c = chain.Next();

            while (char.IsLetter(c.Char))
            {
                typeName += c.Char;
                chain.GetNext();
                c = chain.Next();
            }

            if (!validTypes.Contains(typeName.ToLower()))
            {
                handleError($"Недопустимый тип данных '{typeName}'", typeName, c);
            }

            chain.SkipSpaces();
            c = chain.Next();

            if (c.Char == ';')
            {
                chain.GetNext();
                state = 11;
            }
            else if (c.Char == 'e')
            {
                state = 12;
            }
            else if (c.Char == '\0')
            {
                handleError("Неожиданный конец файла при определении типа", "", c);
                state = 14;
            }
            else if (char.IsLetter(c.Char))
            {
                handleError("Пропущена ';' после определения типа", "", c);
                state = 7;
            }
            else
            {
                handleError("Ожидалась ';' или 'end'", c.Char.ToString(), c);
                state = 11;
            }
        }

        private void state11()
        {
            Character c = chain.Next();

            if (c.Char == 'e')
            {
                state = 12;
            }
            else if (char.IsLetter(c.Char))
            {
                state = 7;
            }
            else if (c.Char == '\0')
            {
                handleError("Неожиданный конец файла", "", c);
                state = 14;
            }
            else
            {
                handleError("Ожидался идентификатор нового поля или 'end'", c.Char.ToString(), c);
                state = 12;
            }
        }

        private void state12()
        {
            string expected = "end";
            string actual = "";
            Character c;

            for (int i = 0; i < expected.Length; i++)
            {
                c = chain.GetNext();
                actual += c.Char;
                if (c.Char != expected[i])
                {
                    handleError($"Ожидалось 'end'. Найдено '{actual}'", actual, c);
                    state = 13;
                    return;
                }
            }

            chain.SkipSpaces();
            c = chain.Next();

            if (c.Char == ';')
            {
                chain.GetNext();
                state = 13;
            }
            else if (c.Char == '\0')
            {
                handleError("Пропущена ';' после 'end'", "", c);
                state = 14;
            }
            else
            {
                handleError("Ожидалась ';' после 'end'", c.Char.ToString(), c);
                state = 13;
            }
        }

        private void state13()
        {
            chain.SkipSpaces();
            Character c = chain.Next();

            if (c.Char == 't')
            {
                state = 1;
            }
            else if (c.Char == '\0')
            {
                state = 14;
            }
            else
            {
                handleError("Ожидалось новое определение типа или конец файла", c.Char.ToString(), c);
                state = 1;
            }
        }
    }
}