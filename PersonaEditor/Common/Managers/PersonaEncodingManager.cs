using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using PersonaEditorLib;

namespace PersonaEditor.Common.Managers
{
    public class PersonaEncodingManager : BindingObject
    {
        Dictionary<string, PersonaEncoding> encodings = new Dictionary<string, PersonaEncoding>()
        {{"Empty", new PersonaEncoding()}};

        string sourcedir;
        ObservableCollection<string> encodingList = new ObservableCollection<string>() { "Empty" };

        public ReadOnlyObservableCollection<string> EncodingList { get; }

        public PersonaEncodingManager(string dir)
        {
            sourcedir = dir;
            EncodingList = new ReadOnlyObservableCollection<string>(encodingList);

            if (Directory.Exists(dir))
            {
                var filelist = Directory.EnumerateFiles(dir);
                foreach (var file in filelist)
                    if (Path.GetExtension(file).Equals(".fntmap", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var temp = Path.GetFileNameWithoutExtension(file);
                        if (temp != "Empty")
                            encodingList.Add(temp);
                    }
            }
        }

        public void Update(string name)
        {
            if (encodings.ContainsKey(name))
                Notify(name);
        }

        public PersonaEncoding GetPersonaEncoding(int index)
        {
            string file = encodingList[index];
            return GetPersonaEncoding(file);
        }

        public PersonaEncoding GetPersonaEncoding(string name)
        {
            if (encodings.ContainsKey(name))
                return encodings[name];
            else
            {
                var enc = new PersonaEncoding(Path.Combine(sourcedir, name + ".fntmap"));
                encodings.Add(name, enc);
                return enc;
            }
        }

        public int GetPersonaEncodingIndex(string name)
        {
            if (encodingList.Contains(name))
                return encodingList.IndexOf(name);
            else
                return -1;
        }

        public string GetPersonaEncodingName(int index)
        {
            if (index >= 0 && index < encodingList.Count)
                return encodingList[index];
            else
                return "";
        }
    }
}