using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using PersonaEditorLib;

namespace PersonaEditor.Common.Managers
{
    public class PersonaFontManager : BindingObject
    {
        Dictionary<string, PersonaFont> encodings = new Dictionary<string, PersonaFont>();

        public string sourcedir { get; }
        ObservableCollection<string> encodingList = new ObservableCollection<string>();

        public ReadOnlyObservableCollection<string> FontList { get; }

        public PersonaFontManager(string dir)
        {
            sourcedir = dir;
            FontList = new ReadOnlyObservableCollection<string>(encodingList);

            if (Directory.Exists(dir))
            {
                var filelist = Directory.EnumerateFiles(dir);
                foreach (var file in filelist)
                    if (Path.GetExtension(file).Equals(".fnt", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var temp = Path.GetFileNameWithoutExtension(file);
                        if (temp != "Empty")
                            encodingList.Add(temp);
                    }
            }
        }

        public PersonaFont GetPersonaFont(int index)
        {
            string file = encodingList[index];

            return GetPersonaFont(file);
        }

        public PersonaFont GetPersonaFont(string name)
        {
            if (encodings.ContainsKey(name))
                return encodings[name];
            else
            {
                try
                {
                    var enc = new PersonaFont(Path.Combine(sourcedir, name + ".fnt"));
                    encodings.Add(name, enc);
                    return enc;
                }
                catch
                {
                    return null;
                }
            }
        }

        public int GetPersonaFontIndex(string name)
        {
            if (encodingList.Contains(name))
                return encodingList.IndexOf(name);
            else
                return -1;
        }

        public string GetPersonaFontName(int index)
        {
            if (index >= 0 && index < encodingList.Count)
                return encodingList[index];
            else
                return "";
        }
    }
}