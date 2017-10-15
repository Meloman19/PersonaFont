﻿using Microsoft.Win32;
using PersonaEditorLib.Extension;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace SPRAtlasEditor
{
    class Key : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected void Notify(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion INotifyPropertyChanged implementation

        private int _X1;
        private int _X2;
        private int _Y1;
        private int _Y2;

        public string Name { get; set; }
        public int X1
        {
            get { return _X1; }
            set
            {
                if (value != _X1)
                {
                    _X1 = value;
                    Notify("X1");
                }
            }
        }
        public int X2
        {
            get { return _X2; }
            set
            {
                if (value != _X2)
                {
                    _X2 = value;
                    Notify("X2");
                }
            }
        }
        public int Y1
        {
            get { return _Y1; }
            set
            {
                if (value != _Y1)
                {
                    _Y1 = value;
                    Notify("Y1");
                }
            }
        }
        public int Y2
        {
            get { return _Y2; }
            set
            {
                if (value != _X1)
                {
                    _Y2 = value;
                    Notify("Y2");
                }
            }
        }
    }

    class Visual
    {
        public DrawingGroup GD = new DrawingGroup();

        GeometryDrawing Brush = new GeometryDrawing();
        GeometryDrawing Boarder = new GeometryDrawing();

        public Key Key { get; private set; }

        public Visual(PersonaEditorLib.FileStructure.SPR.SPRKeyList.SPRKey Key)
        {
            this.Key = new Key()
            {
                Name = Key.mComment,
                X1 = Key.X1,
                X2 = Key.X2,
                Y1 = Key.Y1,
                Y2 = Key.Y2
            };
            this.Key.PropertyChanged += Key_PropertyChanged;

            GD.Children.Add(Boarder);
            GD.Children.Add(Brush);
            Boarder.Geometry = new RectangleGeometry(new Rect(new Point(Key.X1, Key.Y1), new Point(Key.X2, Key.Y2)));
            Brush.Geometry = Boarder.Geometry;
            Boarder.Pen = new Pen(new SolidColorBrush(Current.Default.LineColor), 0.5);
            Boarder.Pen.DashStyle = DashStyles.Dash;
            Current.Default.PropertyChanged += Default_PropertyChanged;
        }

        private void Default_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "LineColor")
            {
                Boarder.Pen.Brush = new SolidColorBrush(Current.Default.LineColor);
            }
            else if (e.PropertyName == "SelectColor")
            {
                if (Brush.Brush != null) Pick();
            }
        }

        private void Key_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Boarder.Geometry = new RectangleGeometry(new Rect(new Point(Key.X1, Key.Y1), new Point(Key.X2, Key.Y2)));
            Brush.Geometry = Boarder.Geometry;
        }

        public void Pick()
        {
            Color color = new Color()
            {
                A = 0x80,
                R = Current.Default.SelectColor.R,
                G = Current.Default.SelectColor.G,
                B = Current.Default.SelectColor.B
            };

            Brush.Brush = new SolidColorBrush(color);
        }

        public void UnPick()
        {
            Brush.Brush = null;
        }
    }

    class DRAW
    {
        public DrawingImage DI { get; private set; }
        public List<Visual> VisualList { get; private set; } = new List<Visual>();

        public DRAW(List<PersonaEditorLib.FileStructure.SPR.SPRKeyList.SPRKey> KeyList, DrawingImage DI)
        {
            this.DI = DI;

            foreach (var a in KeyList)
            {
                Visual D = new Visual(a);
                VisualList.Add(D);
                (DI.Drawing as DrawingGroup).Children.Add(D.GD);
            }
        }
    }

    public partial class MainWindow : Window
    {
        BindingList<string> Names = new BindingList<string>();
        List<DRAW> Images = new List<DRAW>();

        PersonaEditorLib.FileStructure.SPR.SPR SPR;

        public MainWindow()
        {
            Current.Default.Reload();
            InitializeComponent();
            ListNames.DataContext = Names;
            Board.DataContext = Current.Default;
        }

        string OpenFile = "";

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Persona Text Project (*.SPR)|*.SPR";
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == true)
            {
                OpenFile = ofd.FileName;
                Open(ofd.FileName);
            }
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Persona Text Project (*.SPR)|*.SPR";
            sfd.OverwritePrompt = true;
            sfd.InitialDirectory = Path.GetDirectoryName(OpenFile);
            sfd.FileName = Path.GetFileNameWithoutExtension(OpenFile);
            
            if (sfd.ShowDialog() == true)
            {
                Save(sfd.FileName);
            }
        }

        private void Open(string filename)
        {
            Names.Clear();
            Images.Clear();
            SPR = new PersonaEditorLib.FileStructure.SPR.SPR(filename, true);

            BindingList<PersonaEditorLib.FileStructure.TMX.TMX> Textures = new BindingList<PersonaEditorLib.FileStructure.TMX.TMX>();

            foreach (var a in SPR.GetTextureList())
                Textures.Add(new PersonaEditorLib.FileStructure.TMX.TMX(new MemoryStream(a), true));

            for (int i = 0; i < Textures.Count; i++)
            {
                var img = Textures[i];
                Names.Add(Encoding.ASCII.GetString(img.Header.UserComment.Where(x => x != 0).ToArray()));
                var image = img.Data.GetBitmapSource(img.Palette.Pallete);

                var temp = new DrawingImage(new DrawingGroup());

                ImageDrawing ID = new ImageDrawing(image, new Rect(new Size(image.Width, image.Height)));
                (temp.Drawing as DrawingGroup).Children.Add(ID);
                (temp.Drawing as DrawingGroup).ClipGeometry = new RectangleGeometry(ID.Rect);

                Images.Add(new DRAW(SPR.KeyList.List.Where(x => x.mTextureIndex == i).ToList(), temp));
            }

        }

        private void Save(string filename)
        {
            foreach (var a in Images)
            {
                foreach(var b in a.VisualList)
                {
                    var temp = SPR.KeyList.List.Find(x => x.mComment == b.Key.Name);
                    if (temp == null)
                    {
                        MessageBox.Show("Something happened :(");
                    }
                    else
                    {
                        temp.X1 = b.Key.X1;
                        temp.Y1 = b.Key.Y1;
                        temp.X2 = b.Key.X2;
                        temp.Y2 = b.Key.Y2;
                    }

                }
            }

            SPR.Get(true).SaveToFile(filename);
        }

        private void ListNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var temp = sender as ListView;
            MainImage.Source = temp.SelectedIndex == -1 ? null : Images[temp.SelectedIndex].DI;
            KeyList.DataContext = temp.SelectedIndex == -1 ? null : Images[temp.SelectedIndex].VisualList;
        }

        private void SetBackground_Click(object sender, RoutedEventArgs e)
        {
            Color color;
            ColorPickerWPF.ColorPickerWindow.ShowDialog(out color, ColorPickerWPF.Code.ColorPickerDialogOptions.SimpleView);
            Current.Default.BackgroundColor = color;
        }

        private void SetLine_Click(object sender, RoutedEventArgs e)
        {
            Color color;
            ColorPickerWPF.ColorPickerWindow.ShowDialog(out color, ColorPickerWPF.Code.ColorPickerDialogOptions.SimpleView);
            Current.Default.LineColor = color;
        }

        private void SelectColor_Click(object sender, RoutedEventArgs e)
        {
            Color color;
            ColorPickerWPF.ColorPickerWindow.ShowDialog(out color, ColorPickerWPF.Code.ColorPickerDialogOptions.SimpleView);
            Current.Default.SelectColor = color;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Current.Default.Save();
        }

        Visual temp;

        private void KeyList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (temp != null) temp.UnPick();
            if (e.AddedItems.Count > 0)
            {
                temp = (e.AddedItems[0] as Visual);
                temp.Pick();
            }
        }
    }
}