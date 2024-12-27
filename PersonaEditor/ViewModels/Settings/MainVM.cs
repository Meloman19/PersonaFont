using System;
using System.Collections.ObjectModel;
using System.IO;
using PersonaEditor.Common;
using PersonaEditor.Common.Settings;

namespace PersonaEditor.ViewModels.Settings
{
    public sealed class MainVM : BindingObject
    {
        private readonly AppSettings _appSettings;

        public MainVM()
        {
            _appSettings = Static.SettingsProvider.AppSettings.Clone();
            LoadLangList();
        }

        public ObservableCollection<string> LangList { get; } = new ObservableCollection<string>() { "Default" };

        public int SelectedLangIndex
        {
            get
            {
                if (LangList.Contains(_appSettings.DefaultLocalization))
                    return LangList.IndexOf(_appSettings.DefaultLocalization);
                else
                    return 0;
            }
            set
            {
                if (LangList.Count > value)
                    _appSettings.DefaultLocalization = LangList[value];
                Notify(nameof(SelectedLangIndex));
            }
        }

        public ReadOnlyObservableCollection<string> FontList => Static.EncodingManager.EncodingList;

        public bool CopyOld2New
        {
            get { return _appSettings.SaveAsPTP_CO2N; }
            set
            {
                _appSettings.SaveAsPTP_CO2N = value;
                Notify(nameof(CopyOld2New));
            }
        }

        public int SelectedFontSave
        {
            get
            {
                int sourceInd = Static.EncodingManager.GetPersonaEncodingIndex(_appSettings.SaveAsPTP_Font);
                if (sourceInd >= 0)
                    return sourceInd;
                else
                    return 0;
            }
            set
            {
                _appSettings.SaveAsPTP_Font = Static.EncodingManager.GetPersonaEncodingName(value);
                Notify(nameof(SelectedFontSave));
            }
        }

        public int SelectedFontOpen
        {
            get
            {
                int sourceInd = Static.EncodingManager.GetPersonaEncodingIndex(_appSettings.OpenPTP_Font);
                if (sourceInd >= 0)
                    return sourceInd;
                else
                    return 0;
            }
            set
            {
                _appSettings.OpenPTP_Font = Static.EncodingManager.GetPersonaEncodingName(value);
                Notify(nameof(SelectedFontOpen));
            }
        }

        public bool SingleInstanceApplication
        {
            get { return _appSettings.SingleInstanceApplication; }
            set
            {
                _appSettings.SingleInstanceApplication = value;
                Notify(nameof(SingleInstanceApplication));
            }
        }

        public void Save()
        {
            Static.SettingsProvider.AppSettings = _appSettings;
        }

        private void LoadLangList()
        {
            if (Directory.Exists(Static.Paths.DirLang))
            {
                var list = Directory.GetFiles(Static.Paths.DirLang);
                foreach (var file in list)
                    if (Path.GetExtension(file).Equals(".xml", StringComparison.InvariantCultureIgnoreCase))
                        LangList.Add(Path.GetFileNameWithoutExtension(file));
            }
        }
    }
}