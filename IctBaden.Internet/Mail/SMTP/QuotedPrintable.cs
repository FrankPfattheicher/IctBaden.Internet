using System;
using System.Collections.Generic;
using System.Text;
// ReSharper disable UnusedMember.Global
// ReSharper disable UseNameofExpression

namespace IctBaden.Internet.Mail.SMTP
{
    internal class QuotedPrintable
    {
        [Flags]
        public enum DoNotEncode
        {
            Tab = 0x01,
            Space = 0x02
        }

        private readonly bool _encodeSpaces = true;
        private readonly bool _encodeTabs = true;
        private const int MaxLineLength = 76;

        public QuotedPrintable()
        {
        }

        public QuotedPrintable(DoNotEncode doNotEncode)
        {
            _encodeTabs = (doNotEncode & DoNotEncode.Tab) != 0;
            _encodeSpaces = (doNotEncode & DoNotEncode.Space) != 0 ;
        }

        // ReSharper disable once ParameterTypeCanBeEnumerable.Global
        public string Encode(byte[] bytes)
        {
            var sb = new StringBuilder();

            var column = 0;
            string nextOutput = null;

            foreach (var b in bytes)
            {
                nextOutput = ShouldEncode(b) ? "=" + b.ToString("X2") : Char.ToString((char) b);

                if (column + nextOutput.Length >= MaxLineLength)
                {
                    sb.Append("=\n");
                    column = 0;
                }
                sb.Append(nextOutput);
                column += nextOutput.Length;
            }

            if ((nextOutput != null) && (nextOutput.Length == 1) && (nextOutput[0] <= 32))
                sb.Append("=");

            return sb.ToString();
        }

        public byte[] Decode(string text)
        {
            text = text.Replace("=?iso-8859-1?Q?", string.Empty)
                       .Replace("?=", string.Empty)
                       .Replace("_", " ");

            var output = new List<Byte>();
            for (var textIndex = 0; textIndex < text.Length; textIndex++)
            {
                var t = text[textIndex];
                if (t == '=')
                {
                    textIndex++;
                    switch (text.Length - textIndex)
                    {
                        case 1:
                            throw new ArgumentOutOfRangeException("text",
                                @"Only one character found after = sign - is data truncated?");

                        case 0:
                            break;

                        default:
                            output.Add((byte) ((Hex(text[textIndex++]) << 4) + Hex(text[textIndex])));
                            break;
                    }
                }
                else
                    output.Add((byte) t);
            }

            return output.ToArray();
        }

        private bool ShouldEncode(byte b)
        {
            if (b == ' ')
                return _encodeSpaces;
            if (b == (byte) '\t')
                return _encodeTabs;

            return (b < 33) || (b > 126) || (b == '=');
        }

        private int Hex(char a)
        {
            if (a >= '0' && a <= '9')
                return a - '0';

            if (a >= 'a' && a <= 'f')
                return a - 'a' + 10;

            if (a >= 'A' && a <= 'F')
                return a - 'A' + 10;

            throw new ArgumentOutOfRangeException(@"a", $"Character {a} is not hexadecimal");
        }
    }
}