using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using PersonaEditor.Common;
using PersonaEditor.View.Settings;
using PersonaEditor.ViewModels;
using PersonaEditor.ViewModels.Settings;

namespace PersonaEditor.Views
{
    public sealed class MainWindowVM : BindingObject
    {
        private object _mainControlDC = null;

        private Views.Tools.Visualizer _visualizerTool;
        private Views.Tools.SetChar _setcharTool;

        public MainWindowVM()
        {
            WindowClosingCommand = new RelayCommand(WindowClosing);
            OpenFileCommand = new RelayCommand(OpenFile);
            SaveFileAsCommand = new RelayCommand(SaveFileAs);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            OpenVisualizerCommand = new RelayCommand(OpenVisualizer);
            OpenSetCharCommand = new RelayCommand(OpenSetChar);

            OpenAboutCommand = new RelayCommand(OpenAbout);
        }

        public MultiFileEditVM MultiFile { get; } = new MultiFileEditVM();

        public object MainControlDC
        {
            get => _mainControlDC;
            set => SetProperty(ref _mainControlDC, value);
        }

        public ICommand WindowClosingCommand { get; }

        public ICommand OpenFileCommand { get; }

        public ICommand SaveFileAsCommand { get; }

        public ICommand OpenVisualizerCommand { get; }

        public ICommand OpenSetCharCommand { get; }

        public ICommand OpenSettingsCommand { get; }

        public ICommand OpenAboutCommand { get; }

        private void WindowClosing(object arg)
        {
            if (MultiFile.CloseFile())
            {
                if (_visualizerTool != null)
                    _visualizerTool.Close();
                if (_setcharTool != null)
                    _setcharTool.Close();

                Static.SettingsProvider.Save();
            }
            else
                (arg as CancelEventArgs).Cancel = true;
        }

        private void OpenFile()
        {
            OpenFileDialog OFD = new OpenFileDialog();
            if (OFD.ShowDialog() == true)
                MultiFile.OpenFile(OFD.FileName);
        }

        private void SaveFileAs()
        {
            if (MultiFile.OpenFileName != "")
            {
                SaveFileDialog SFD = new SaveFileDialog();
                SFD.OverwritePrompt = true;

                string dirpath = Path.GetDirectoryName(MultiFile.OpenFileName);
                string filename = Path.GetFileName(MultiFile.OpenFileName);
                if (Directory.Exists(dirpath))
                    SFD.InitialDirectory = dirpath;
                SFD.FileName = filename;

                string ext = Path.GetExtension(MultiFile.OpenFileName).Remove(0, 1);
                SFD.Filter = ext.ToUpperInvariant() + "|*." + ext;

                if (SFD.ShowDialog() == true)
                    MultiFile.SaveFile(SFD.FileName);
            }
        }

        private void OpenVisualizer()
        {
            if (_visualizerTool != null)
                if (_visualizerTool.IsLoaded)
                {
                    _visualizerTool.Activate();
                    return;
                }

            _visualizerTool = new Views.Tools.Visualizer() { DataContext = new ViewModels.Tools.VisualizerVM() };
            _visualizerTool.Show();
        }

        private void OpenSetChar()
        {
            if (_setcharTool != null)
                if (_setcharTool.IsLoaded)
                {
                    _setcharTool.Activate();
                    return;
                }

            _setcharTool = new Views.Tools.SetChar() { DataContext = new ViewModels.Tools.SetCharVM() };
            _setcharTool.Show();
        }

        private void OpenSettings()
        {
            Static.SettingsProvider.Save();
            SetSettings setSettings = new SetSettings() { DataContext = new SetSettingsVM() };
            setSettings.ShowDialog();
        }

        private void OpenAbout()
        {
#if DEBUG
            TestClick();
#endif

            (new About()).ShowDialog();
        }

        private void TestClick()
        {
        }

        public void OpenFile(string path)
        {
            if (File.Exists(path))
                MultiFile.OpenFile(path);
        }
    }
}