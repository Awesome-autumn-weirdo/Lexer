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

        public Character(char c, int idx)
        {
            Char = c;
            Idx = idx;
        }
    }

    class CharChain
    {
        private readonly char[] chars;
        private int index;
        private readonly int maxLength;

        public CharChain(string text)
        {
            chars = text.ToCharArray();
            index = 0;
            maxLength = chars.Length;
        }

        public Character GetNext()
        {
            if (index >= maxLength)
                return new Character('\0', index);

            return new Character(chars[index++], index - 1);
        }

        public Character Next()
        {
            if (index >= maxLength)
                return new Character('\0', index);

            return new Character(chars[index], index);
        }

        public void SkipSpaces()
        {
            while (index < maxLength && char.IsWhiteSpace(chars[index]))
                index++;
        }
    }

    class RecordTypeParser
    {
        private CharChain chain;
        private int state;
        private readonly List<ParseError> errors;
        private int iterationCount;
        private const int MaxIterations = 1000; // Защита от бесконечного цикла

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

            chain.SkipSpaces();

            while (state != 14 && iterationCount < MaxIterations)
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
                }

                chain.SkipSpaces();
            }

            if (iterationCount >= MaxIterations)
            {
                errors.Add(new ParseError("Превышено максимальное количество итераций", "", chain.Next().Idx));
            }

            return errors.Count == 0;
        }

        private void handleError(string msg, string removed, Character c)
        {
            errors.Add(new ParseError(msg, removed, c.Idx));
            state = 14; // Переходим в конечное состояние при ошибке
        }

        private readonly HashSet<string> validTypes = new HashSet<string>
        {
            "integer", "real", "char", "boolean", "string", "var"
        };

        private bool tryStop()
        {
            char next = chain.Next().Char;
            if (next == '\0' || next == ';')
            {
                chain.GetNext();
                state = 14;
                return true;
            }
            return false;
        }

        private void state1()
        {
            Character c = chain.GetNext();
            if (c.Char == 't')
                state = 2;
            else
                handleError("Ожидалось 'type'.", c.Char.ToString(), c);
        }

        private void state2()
        {
            Character c = chain.Next();
            if (c.Char != 'y') { handleError("Ожидалось 'type'.", c.Char.ToString(), c); return; }
            chain.GetNext();

            c = chain.Next();
            if (c.Char != 'p') { handleError("Ожидалось 'type'.", c.Char.ToString(), c); return; }
            chain.GetNext();

            c = chain.Next();
            if (c.Char != 'e') { handleError("Ожидалось 'type'.", c.Char.ToString(), c); return; }
            chain.GetNext();

            state = 3;
        }

        private void state3()
        {
            Character c = chain.GetNext();
            if (char.IsLetter(c.Char))
                state = 4;
            else
                handleError("Ожидался идентификатор.", c.Char.ToString(), c);
        }

        private void state4()
        {
            Character c = chain.GetNext();
            if (char.IsLetterOrDigit(c.Char))
                state = 4;
            else if (c.Char == '=')
                state = 5;
            else
                handleError("Ожидался символ '='.", c.Char.ToString(), c);
        }

        private void state5()
        {
            Character c = chain.GetNext();
            if (c.Char == 'r')
                state = 6;
            else
                handleError("Ожидалось 'record'.", c.Char.ToString(), c);
        }

        private void state6()
        {
            if (chain.Next().Char == 'e' && chain.GetNext().Char == 'e' &&
                chain.Next().Char == 'c' && chain.GetNext().Char == 'c' &&
                chain.Next().Char == 'o' && chain.GetNext().Char == 'o' &&
                chain.Next().Char == 'r' && chain.GetNext().Char == 'r' &&
                chain.Next().Char == 'd' && chain.GetNext().Char == 'd')
            {
                state = 7;
                chain.SkipSpaces();
            }
            else
                handleError("Ожидалось 'record'.", "", chain.GetNext());
        }

        private void state7()
        {
            Character c = chain.GetNext();
            if (char.IsLetter(c.Char))
                state = 8;
            else
                handleError("Ожидался идентификатор поля.", c.Char.ToString(), c);
        }

        private void state8()
        {
            Character c = chain.GetNext();

            if (char.IsLetterOrDigit(c.Char))
            {
                state = 8; // Остаемся в этом же состоянии, пока продолжается имя переменной
            }
            else if (c.Char == ',')
            {
                state = 7; // Переход к следующему идентификатору
            }
            else if (c.Char == ':')
            {
                state = 9; // Переход к типу данных
            }
            else
            {
                handleError("Ожидалась ',' или ':'.", c.Char.ToString(), c);
            }
        }


        private void state9()
        {
            Character c = chain.Next();
            if (char.IsLetter(c.Char))
                state = 10;
            else
                handleError("Ожидался тип данных.", c.Char.ToString(), c);
        }

        private void state10()
        {
            string typeName = "";
            Character c = chain.Next();

            while (char.IsLetter(c.Char)) // Читаем название типа
            {
                typeName += c.Char;
                chain.GetNext();
                c = chain.Next();
            }

            if (!validTypes.Contains(typeName.ToLower()))
            {
                handleError($"Недопустимый тип данных '{typeName}'.", typeName, c);
                return;
            }

            chain.SkipSpaces();
            c = chain.Next();

            if (c.Char == ';') // Если есть ';' — переходим к следующему полю
            {
                chain.GetNext();
                state = 11;
            }
            else if (c.Char == 'e') // Если следующий символ 'e' (начало 'end'), переходим к завершению
            {
                state = 12;
            }
            else
            {
                handleError("Ожидалась ';' или 'end'.", c.Char.ToString(), c);
            }
        }


        private void state11()
        {
            Character c = chain.Next(); // Смотрим следующий символ

            if (c.Char == 'e')
            {
                state = 12; // Переход к завершению record (end;)
            }
            else if (char.IsLetter(c.Char))
            {
                state = 7; // Переход к следующему идентификатору
            }
            else
            {
                handleError("Ожидался идентификатор нового поля или 'end'.", c.Char.ToString(), c);
            }
        }

        private void state12()
        {
            // Проверяем 'e'
            Character c = chain.GetNext();
            if (c.Char != 'e')
            {
                handleError("Ожидалось 'end'.", c.Char.ToString(), c);
                return;
            }

            // Проверяем 'n'
            c = chain.GetNext();
            if (c.Char != 'n')
            {
                handleError("Ожидалось 'end'.", c.Char.ToString(), c);
                return;
            }

            // Проверяем 'd'
            c = chain.GetNext();
            if (c.Char != 'd')
            {
                handleError("Ожидалось 'end'.", c.Char.ToString(), c);
                return;
            }

            chain.SkipSpaces();

            // Проверяем ';'
            c = chain.Next();
            if (c.Char == ';')
            {
                chain.GetNext();
                state = 14; // Успешный конец
            }
            else
            {
                handleError("Ожидалась ';' после 'end'.", c.Char.ToString(), c);
            }
        }


        private void state13()
        {
            if (tryStop())
                return;

            Character c = chain.GetNext();
            handleError("Неожиданный символ после 'end;'", c.Char.ToString(), c);
        }

    }
}