using System;
using System.Text;

namespace PersonaEditorLib.Text
{
    public struct TextBaseElement
    {
        public TextBaseElement(bool isText, byte[] array)
        {
            Data = array;
            IsText = isText;            
        }

        public string GetText(Encoding encoding, bool linesplit = false)
        {
            if (IsText)
                return String.Concat(encoding.GetChars(Data));
            else
            {
                if (Data[0] == 0x0A)
                    if (linesplit)
                        return "\n";
                    else
                        return GetSystem();
                else
                    return GetSystem();
            }
        }

        public string GetSystem()
        {
            string returned = "";

            if (Data.Length > 0)
            {
                returned += "{" + Convert.ToString(Data[0], 16).PadLeft(2, '0').ToUpperInvariant();
                for (int i = 1; i < Data.Length; i++)
                    returned += " " + Convert.ToString(Data[i], 16).PadLeft(2, '0').ToUpperInvariant();

                returned += "}";
            }

            return returned;
        }

        public bool IsText { get; }
        public byte[] Data { get; }
    }
}