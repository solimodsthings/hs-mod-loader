using Microsoft.Win32;
using Ookii.Dialogs.WinForms;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace HSModLoader.App.Publishing
{
    /// <summary>
    /// This is a dialog window that allows a user to set the
    /// path to the game.
    /// </summary>
    public partial class NewModWindow : Window
    {
        private static readonly string PlaceholderModName = "New Mod ";

        public Mod ResultMod { get; set; }
        public string ResultDirectory { get; set; }

        public NewModWindow()
        {
            InitializeComponent();

            var dir = Directory.GetCurrentDirectory();
            this.TextBoxModLocation.Text = dir;

            int index = 1;
            var suggestedModName = PlaceholderModName + index;
            var path = System.IO.Path.Combine(dir, suggestedModName);
            
            while (File.Exists(path) || Directory.Exists(path))
            {
                index++;
                suggestedModName = PlaceholderModName + index;
                path = System.IO.Path.Combine(dir, suggestedModName);
            }

            this.TextBoxModName.Text = suggestedModName;

        }

        private void OnBrowseButtonClick(object sender, RoutedEventArgs e)
        {
            var folder = new VistaFolderBrowserDialog();

            folder.ShowNewFolderButton = true;
            folder.RootFolder = Environment.SpecialFolder.MyComputer;

            var result = folder.ShowDialog();

            if(result == System.Windows.Forms.DialogResult.OK)
            {
                this.TextBoxModLocation.Text = folder.SelectedPath;
            }
        }

        private void OnCreateButtonClick(object sender, RoutedEventArgs e)
        {
            var modName = this.TextBoxModName.Text;
            var parentFolder = this.TextBoxModLocation.Text;

            if (string.IsNullOrEmpty(modName) || string.IsNullOrEmpty(parentFolder))
            {
                // TODO show a warning
                return;
            }

            if (!Directory.Exists(parentFolder))
            {
                // TODO show a warning
                return;
            }

            var newFolder = System.IO.Path.Combine(parentFolder, modName);

            if (Directory.Exists(newFolder))
            {
                // TODO show a warning
                return;
            }

            Directory.CreateDirectory(newFolder);
            this.ResultMod = new Mod() { Name = modName };
            this.ResultDirectory = newFolder;
            this.DialogResult = true;
            this.Close();

        }
    }
}
