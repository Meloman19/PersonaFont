using AuxiliaryLibraries.Extensions;
using AuxiliaryLibraries.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PersonaEditorLib.Text
{
    public static class Extension
    {
        public static IEnumerable<TextBaseElement> GetTextBases(this string s, Encoding enc)
        {
            List<TextBaseElement> MyByteArrayList = new List<TextBaseElement>();

            foreach (var a in Regex.Split(s, "(\r\n|\r|\n)"))
                if (Regex.IsMatch(a, "\r\n|\r|\n"))
                    yield return new TextBaseElement(false, new byte[] { 0x0A });
                else
                    foreach (var b in Regex.Split(a, @"({[^}]+})"))
                        if (Regex.IsMatch(b, @"{.+}") && StringTool.TryParseArray(b.Substring(1, b.Length - 2), out byte[] parsed))
                            yield return new TextBaseElement(false, parsed);
                        else
                            yield return new TextBaseElement(true, enc.GetBytes(b));
        }

        public static IEnumerable<TextBaseElement> GetTextBases(this byte[] array)
        {
            List<TextBaseElement> returned = new List<TextBaseElement>();

            List<byte> temp = new List<byte>();

            for (int i = 0; i < array.Length; i++)
            {
                if (0x20 <= array[i] & array[i] < 0x80)
                {
                    temp.Add(array[i]);
                }
                else if (0x80 <= array[i] & array[i] < 0xF0)
                {
                    temp.Add(array[i]);
                    i++;
                    temp.Add(array[i]);
                }
                else
                {
                    if (0x00 <= array[i] & array[i] < 0x20)
                    {
                        if (temp.Count != 0)
                        {
                            yield return new TextBaseElement(true, temp.ToArray());
                            temp.Clear();
                        }

                        temp.Add(array[i]);
                        yield return new TextBaseElement(false, temp.ToArray());
                        temp.Clear();
                    }
                    else
                    {
                        if (temp.Count != 0)
                        {
                            yield return new TextBaseElement(true, temp.ToArray());
                            temp.Clear();
                        }

                        temp.Add(array[i]);
                        int count = (array[i] - 0xF0) * 2 - 1;
                        for (int k = 0; k < count; k++)
                        {
                            i++;
                            temp.Add(array[i]);
                        }

                        yield return new TextBaseElement(false, temp.ToArray());
                        temp.Clear();
                    }
                }
            }

            if (temp.Count != 0)
            {
                yield return new TextBaseElement(true, temp.ToArray());
                temp.Clear();
            }
        }

        public static List<string> SplitBySystem(this string str)
        {
            List<string> returned = new List<string>();

            foreach (var a in Regex.Split(str, "(\r\n|\r|\n)"))
                if (Regex.IsMatch(a, "\r\n|\r|\n"))
                    returned.Add("{0A}");
                else
                    foreach (var b in Regex.Split(a, @"({[^}]+})"))
                        returned.Add(b);

            return returned;
        }
        
        public static string SplitByWidth(this string String, Dictionary<char, int> charWidth, int width)
        {
            var temp = GetStringWidth(String, charWidth);
            List<string> tempStr = temp.Item1;
            List<int> tempWidth = temp.Item2;

            List<string> result = new List<string>();
            string input = "";
            int widthsum = 0;
            for (int i = 0; i < tempStr.Count; i++)
            {
                if (widthsum == 0)
                {
                    if (tempStr[i] != " ")
                    {
                        widthsum += tempWidth[i];
                        input += tempStr[i];
                    }
                    else if (i + 1 < tempStr.Count & tempStr[i + 1].Equals("{0A}", StringComparison.CurrentCultureIgnoreCase))
                    {
                        widthsum += tempWidth[i];
                        input += tempStr[i];
                    }
                }
                else
                {
                    if (widthsum + tempWidth[i] > width)
                    {
                        result.Add(input);
                        i--;
                        widthsum = 0;
                        input = "";
                    }
                    else
                    {
                        widthsum += tempWidth[i];
                        input += tempStr[i];
                    }
                }
            }
            if (input != "")
                result.Add(input);

            return string.Join("\n", result);
        }

        public static string SplitByLineCount(this string String, Dictionary<char, int> charWidth, int lineCount)
        {
            var temp = GetStringWidth(String, charWidth);

            List<string> tempStr = temp.Item1;
            List<int> tempWidth = temp.Item2;

            List<int> indexies = new List<int>();
            List<string> returned = new List<string>();
            int width = tempWidth.Sum() / lineCount;
            int tempwidth = 0;
            int tempind = 0;
            for (int i = 0; i < tempWidth.Count; i++)
            {
                if (tempwidth + tempWidth[i] > width)
                {
                    indexies.Add(tempind);
                    tempind = i + 1;
                    tempwidth = 0;
                    if (indexies.Count == lineCount)
                        break;
                }
                else
                    tempwidth += tempWidth[i];
            }
            if (indexies.Count != lineCount)
                indexies.Add(tempind);

            var splitedByLineCount = String.Join("\n", tempStr.ToArray().Split(indexies.ToArray()).Select(x => String.Join("", x)).Select(x => x.TrimStart(' ')));

            return splitedByLineCount;
        }

        private static (List<string>, List<int>) GetStringWidth(string str, Dictionary<char, int> charWidth)
        {
            string input = String.Join(" ", Regex.Split(str, @"\\n|\r\n|\r|\n"));

            List<string> tempStr = new List<string>();
            List<bool> tempBool = new List<bool>();
            List<int> tempWidth = new List<int>();

            var split = input.SplitBySystem();
            foreach (var a in split)
            {
                try
                {
                    StringTool.SplitString(a.Substring(1, a.Length - 2), ' ');
                    tempStr.Add(a);
                    tempBool.Add(false);
                }
                catch
                {
                    foreach (var b in Regex.Split(a, @"( )").Where(x => x != ""))
                    {
                        tempStr.Add(b);
                        tempBool.Add(true);
                    }
                }
            }

            for (int i = 0; i < tempStr.Count; i++)
            {
                if (tempBool[i])
                {
                    int temp = 0;
                    foreach (var a in tempStr[i])
                        if (charWidth.ContainsKey(a))
                            temp += charWidth[a];

                    tempWidth.Add(temp);
                }
                else
                {
                    if (tempStr[i].Equals("{F1 81}") | tempStr[i].Equals("{F1 82}") | tempStr[i].Equals("{F1 83}"))
                        tempWidth.Add(10);
                    else
                        tempWidth.Add(0);
                }
            }

            return (tempStr, tempWidth);
        }

        public static string MSGListToSystem(this IList<TextBaseElement> list)
        {
            string returned = "";
            foreach (var Bytes in list)
            {
                byte[] temp = Bytes.Data.ToArray();
                if (temp.Length > 0)
                {
                    returned += "{" + System.Convert.ToString(temp[0], 16).PadLeft(2, '0').ToUpperInvariant();
                    for (int i = 1; i < temp.Length; i++)
                    {
                        returned += "\u00A0" + System.Convert.ToString(temp[i], 16).PadLeft(2, '0').ToUpperInvariant();
                    }
                    returned += "} ";
                }
            }
            return returned;
        }

        public static string GetString(this IEnumerable<TextBaseElement> textBases, Encoding encoding, bool lineSplit = false)
        {
            string returned = "";

            foreach (var MSG in textBases)
                returned += MSG.GetText(encoding, lineSplit);

            return returned;
        }

        public static byte[] GetByteArray(this IEnumerable<TextBaseElement> textBaseElements)
        {
            if (textBaseElements == null)
                throw new ArgumentNullException(nameof(textBaseElements));

            List<byte> temp = new List<byte>();
            foreach (var textBase in textBaseElements)
                temp.AddRange(textBase.Data);
            return temp.ToArray();
        }
    }
}