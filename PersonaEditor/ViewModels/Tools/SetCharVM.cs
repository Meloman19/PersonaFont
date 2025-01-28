using PersonaEditorLib;
using AuxiliaryLibraries.WPF;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using PersonaEditor.Common;
using AuxiliaryLibraries.Media;
using AuxiliaryLibraries.WPF.Wrapper;
using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace PersonaEditor.ViewModels.Tools
{
    class SetCharVM : BindingObject
    {
        public class FnMpImg : BindingObject
        {
            private string _char = "";
            private BitmapSource _Image;

            public int Index { get; set; } = 0;

            public string Char
            {
                get => _char;
                set => SetProperty(ref _char, value);
            }

            public BitmapSource Image
            {
                get => _Image;
                set => SetProperty(ref _Image, value);
            }
        }

        private int _FontSelect = -1;
        public ReadOnlyObservableCollection<string> FontList => Static.FontManager.FontList;

        public int FontSelect
        {
            get { return _FontSelect; }
            set
            {
                if (Save())
                {
                    _FontSelect = value;
                    GlyphListUpdate();
                }
                else { Notify("FontSelect"); }
            }
        }

        public BindingList<FnMpImg> GlyphList { get; } = new BindingList<FnMpImg>();

        public SetCharVM()
        {
            Closing = new RelayCommand(Window_Closing);
            GlyphList.ListChanged += GlyphList_ListChanged;
            ExternalEdit = new RelayCommand(ExternalEdit_Click);
        }

        private void GlyphList_ListChanged(object sender, ListChangedEventArgs e)
        {
            IsChanged = true;
            GlyphList.ListChanged -= GlyphList_ListChanged;
        }

        bool IsChanged = false;

        private void GlyphListUpdate()
        {
            GlyphList.ListChanged -= GlyphList_ListChanged;
            GlyphList.Clear();
            var font = Static.FontManager.GetPersonaFont(_FontSelect);
            if (font != null)
                foreach (var a in font.DataList)
                {
                    var pixelMap = new PixelMap(font.Width, font.Height, a.Value);
                    var temp = new FnMpImg()
                    {
                        Index = a.Key,
                        Image = pixelMap.GetBitmapSource()
                    };
                    GlyphList.Add(temp);
                }
            var enc = Static.EncodingManager.GetPersonaEncoding(Static.FontManager.GetPersonaFontName(_FontSelect));

            foreach (var a in GlyphList)
                if (enc.Dictionary.ContainsKey(a.Index))
                    a.Char = enc.Dictionary[a.Index].ToString();

            GlyphList.ListChanged += GlyphList_ListChanged;
        }

        public ICommand ExternalEdit { get; }

        public void ExternalEdit_Click()
        {
            var enc = Static.EncodingManager.GetPersonaEncoding(Static.FontManager.GetPersonaFontName(_FontSelect));

            if (enc.Tag != "")
            {
                string[] charList = new string[GlyphList.Last().Index + 1];
                for (int i = 0; i < 32; i++)
                    charList[i] = "\\u" + i.ToString("x4");
                foreach (var a in GlyphList)
                    charList[a.Index] = a.Char;

                // Create tsv file
                var path = Path.Combine(Static.Paths.DirFont, enc.Tag + ".tsv");
                using (var file = File.Create(path))
                {
                    using (var writer = new StreamWriter(file))
                    {
                        for (int i = 0; i < charList.Length; i++)
                        {
                            writer.Write(charList[i]);
                            if (i % 16 == 15)
                                writer.WriteLine();
                            else if (i != charList.Length - 1)
                                writer.Write('\t');
                        }
                    }
                }

                // Open tsv file on external editor
                ProcessStartInfo psi = new ProcessStartInfo(path);
                psi.UseShellExecute = true;
                Process.Start(psi);

                var result = MessageBox.Show("Load file changes from external tsv file?", "Open in External Editor", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    var modifications = new List<(int, string)>();

                    // Read tsv file
                    using (var file = File.OpenRead(path))
                    {
                        using (var reader = new StreamReader(file))
                        {
                            int i = 0;
                            while (!reader.EndOfStream)
                            {
                                var line = reader.ReadLine();
                                var chars = line.Split('\t');
                                foreach (var a in chars)
                                {
                                    if (charList[i] != a)
                                        modifications.Add((i, a));
                                    i++;
                                    if (i >= charList.Length)
                                        break;
                                }
                            }
                        }
                    }

                    if (modifications.Count == 0)
                    {
                        MessageBox.Show("No changes found.", "Open in External Editor", MessageBoxButton.OK);
                        return;
                    }

                    // Confirm changes
                    string confirmMessage = "Confirm following " + modifications.Count + " change(s)?";
                    for (int i = 0; i < Math.Min(modifications.Count, 10); i++)
                    {
                        confirmMessage += "\n[" + modifications[i].Item1 + "] \"" + charList[modifications[i].Item1] + "\" -> \"" + modifications[i].Item2 + "\"";
                    }
                    if (modifications.Count > 10)
                        confirmMessage += "\n...";
                    var confirmResult = MessageBox.Show(confirmMessage, "Open in External Editor", MessageBoxButton.YesNo);

                    if (confirmResult == MessageBoxResult.Yes)
                    {
                        foreach (var a in modifications)
                        {
                            GlyphList[a.Item1 - 32].Char = a.Item2;
                        }
                    }
                }
            }
        }

        bool Save()
        {
            if (IsChanged)
            {
                var result = MessageBox.Show("Save changes?", "Save", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);

                if (result == MessageBoxResult.Yes)
                {
                    var enc = Static.EncodingManager.GetPersonaEncoding(Static.FontManager.GetPersonaFontName(_FontSelect));
                    if (enc.Tag == "Empty")
                    {
                        PersonaEncoding personaEncoding = new PersonaEncoding();
                        foreach (var a in GlyphList)
                            if (a.Char.Length > 0)
                                personaEncoding.Add(a.Index, a.Char[0]);

                        personaEncoding.SaveFNTMAP(Path.Combine(Static.FontManager.sourcedir, Static.FontManager.GetPersonaFontName(_FontSelect) + ".FNTMAP"));
                    }
                    else
                    {
                        foreach (var a in GlyphList)
                        {
                            if (a.Char.Length > 0)
                                enc.Add(a.Index, a.Char[0]);
                            else
                                enc.Remove(a.Index);
                        }
                        enc.SaveFNTMAP(enc.FilePath);
                        Static.EncodingManager.Update(Static.FontManager.GetPersonaFontName(_FontSelect));
                    }
                }
                else if (result == MessageBoxResult.Cancel)
                    return false;

                IsChanged = false;
                return true;
            }
            else return true;
        }

        public ICommand Closing { get; }

        void Window_Closing(object arg)
        {
            if (!Save())
                (arg as CancelEventArgs).Cancel = true;
        }
    }
}