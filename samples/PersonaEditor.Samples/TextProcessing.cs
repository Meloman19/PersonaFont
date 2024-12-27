using AuxiliaryLibraries.Tools;
using PersonaEditorLib;
using PersonaEditorLib.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PersonaEditor.Samples
{
    /*
    usually raw data contains text and system blocks.
    we don't usually need system blocks at the beginning and at the end.
    we are only interested in text blocks on middle.
    this simple class split raw data to prefix|body|postfix.
    */
    public sealed class MSGSplitter
    {
        public List<TextBaseElement> Prefix { get; } = new List<TextBaseElement>();
        public List<TextBaseElement> Body { get; } = new List<TextBaseElement>();
        public List<TextBaseElement> Postfix { get; } = new List<TextBaseElement>();

        public MSGSplitter(byte[] data)
        {
            var bases = data.GetTextBases().ToList();

            var ind = 0;
            while (bases.Count > 0)
            {
                var b = bases[ind];
                if (b.IsText)
                    break;

                Prefix.Add(b);
                bases.RemoveAt(ind);
            }

            ind = bases.Count - 1;
            while (bases.Count > 0)
            {
                var b = bases[ind];
                if (b.IsText)
                    break;

                Postfix.Add(b);
                bases.RemoveAt(ind);
                ind = bases.Count - 1;
            }
            Postfix.Reverse();

            Body.AddRange(bases);
        }

        public void ChangeBody(string str, Encoding newEncoding)
        {
            var newBody = str.GetTextBases(newEncoding);
            Body.Clear();
            Body.AddRange(newBody);
        }

        public void ChangeEncoding(Encoding oldEncoding, Encoding newEncoding)
        {
            List<TextBaseElement> newBody = new List<TextBaseElement>();
            foreach (var el in Body)
            {
                if (el.IsText)
                {
                    var str = oldEncoding.GetString(el.Data);
                    var newData = newEncoding.GetBytes(str);
                    newBody.Add(new TextBaseElement(true, newData));
                }
                else
                {
                    newBody.Add(el);
                }
            }
            Body.Clear();
            Body.AddRange(newBody);
        }

        // by default argument linesplit is false. it's means that all linebreak transform to text "{0A}"
        // otherwise linebreak output as is.
        public string GetBodyText(Encoding encoding, bool lineSplit = false) => Body.GetString(encoding, lineSplit);

        public byte[] GetData() => Prefix.Concat(Body).Concat(Postfix).GetByteArray();
    }

    public static class TextProcessing
    {
        public static void ExportAllText(string inputDir, string outputDir, string fntmapPath)
        {
            var encoding = new PersonaEncoding(fntmapPath);

            // Get text splitted by directory and file.
            // Output data must have two indexies and text
            // How to use indexies see in import.
            var allText = new Dictionary<string, Dictionary<string, (int, int, string)[]>>();

            foreach (var file in Directory.EnumerateFiles(inputDir, "*", SearchOption.AllDirectories))
            {
                // Try open file as known format;
                var gf = GameFormatHelper.OpenUnknownFile(Path.GetFileName(file), File.ReadAllBytes(file));

                // Collect all BMD
                var bmdGFs = gf.GetAllObjectOfType<BMD>().ToArray();
                if (!bmdGFs.Any())
                    continue;

                // Get path to file relative to root
                var relFilePath = IOTools.RelativePath(file, inputDir);
                var relDirPath = Path.GetDirectoryName(relFilePath);

                foreach (var bmdGF in bmdGFs)
                {
                    var bmd = bmdGF.GameData as BMD;

                    var fileText = new List<(int, int, string)>();

                    // BMD contains many messages with many strings
                    for (int i = 0; i < bmd.Msg.Count; i++)
                    {
                        var msg = bmd.Msg[i];
                        for (var k = 0; k < msg.MsgStrings.Length; k++)
                        {
                            var str = msg.MsgStrings[k];
                            var splitter = new MSGSplitter(str);

                            if (!splitter.Body.Any())
                                continue;

                            var text = splitter.GetBodyText(encoding);

                            fileText.Add((i, k, text));
                        }
                    }

                    if (!fileText.Any())
                        continue;

                    var newRelFilePath = Path.Combine(relDirPath, bmdGF.Name).ToUpperInvariant();
                    var newRelDirPath = Path.GetDirectoryName(newRelFilePath);
                    var newFileName = Path.GetFileName(newRelFilePath);

                    if (!allText.TryGetValue(newRelDirPath, out var dict))
                    {
                        dict = new Dictionary<string, (int, int, string)[]>();
                        allText[newRelDirPath] = dict;
                    }

                    dict[newFileName] = fileText.ToArray();
                }
            }

            // now grouping text by directory in single TSV file.
            foreach (var dirPair in allText)
            {
                var name = dirPair.Key.ToUpperInvariant().Replace('\\', '_') + ".tsv";
                var output = Path.Combine(outputDir, name);

                var outputText = dirPair.Value
                    .SelectMany(x =>
                        x.Value.Select(y =>
                            string.Join('\t', x.Key.ToUpperInvariant(), y.Item1, y.Item2, y.Item3)))
                    .ToArray();

                File.WriteAllLines(output, outputText);
            }
        }

        public static void ImportAllText(BMD bmdToImport, PersonaEncoding oldEncoding, PersonaEncoding newEncoding, (int, int, string)[] transl)
        {
            for (int i = 0; i < bmdToImport.Msg.Count; i++)
            {
                var msg = bmdToImport.Msg[i];
                for (var k = 0; k < msg.MsgStrings.Length; k++)
                {
                    var str = msg.MsgStrings[k];
                    var splitter = new MSGSplitter(str);

                    var translation = transl.FirstOrDefault(x => x.Item1 == i && x.Item2 == k).Item3;
                    if (string.IsNullOrEmpty(translation))
                    {
                        // if there is no translation, it leaves the text as it is.
                        // but you definitely need to change the encoding.
                        splitter.ChangeEncoding(oldEncoding, newEncoding);
                    }
                    else
                    {
                        splitter.ChangeBody(translation, newEncoding);
                    }

                    msg.MsgStrings[k] = splitter.GetData();
                }
            }
        }
    }
}