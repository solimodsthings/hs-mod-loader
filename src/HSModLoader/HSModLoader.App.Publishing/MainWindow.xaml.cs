using Ookii.Dialogs.WinForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HSModLoader.App.Publishing
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private ModContext CurrentModContext { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            this.CurrentModContext = new ModContext();
            this.DataContext = this.CurrentModContext;
        }

        private void ShowOverlay(bool show)
        {
            if (show)
            {
                this.CanvasFadeOut.Visibility = Visibility.Visible;
            }
            else
            {
                this.CanvasFadeOut.Visibility = Visibility.Collapsed;
            }
        }
        private void ShowProgressOverlay(bool show)
        {
            if (show)
            {
                this.ProgressRing.Visibility = Visibility.Visible;
            }
            else
            {
                this.ProgressRing.Visibility = Visibility.Collapsed;
            }
        }

        private void Refresh()
        {
            if (this.CurrentModContext.Mod == null)
            {
                this.InfoPanel.IsEnabled = false;
                this.FileListPanel.IsEnabled = false;
            }
            else
            {
                this.InfoPanel.IsEnabled = true;
                this.FileListPanel.IsEnabled = true;
            }
        }

        private void ShowPopupMessage(string header, string body)
        {
            this.ShowOverlay(true);
            var dialog = new MessageWindow(header, body);
            dialog.Owner = this;

            dialog.ShowDialog();

            this.ShowOverlay(false);

        }


        private void Save()
        {
            this.ShowOverlay(true);

            this.Focus();

            var mod = this.CurrentModContext.Mod;
            if (mod != null)
            {
                var json = JsonSerializer.Serialize(mod, new JsonSerializerOptions() { WriteIndented = true });
                var path = System.IO.Path.Combine(this.CurrentModContext.Directory, ModManager.ModInfoFile);
                File.WriteAllText(path, json);
            }

            this.ShowOverlay(false);
        }

        private void OnNewModButtonClick(object sender, RoutedEventArgs e)
        {
            this.ShowOverlay(true);

            var creation = new NewModWindow() { Owner = this };;
            var result = creation.ShowDialog();

            if(result == true)
            {
                this.CurrentModContext.Mod = creation.ResultMod;
                this.CurrentModContext.Directory = creation.ResultDirectory;
                this.Refresh();
            }

            this.ShowOverlay(false);
        }

        private void OnOpenModButtonClick(object sender, RoutedEventArgs e)
        {
            if(this.CurrentModContext.Mod != null)
            {
                // TODO Prompt user if they actually want to save before opening another mod package folder
                this.Save();
            }

            var folder = new VistaFolderBrowserDialog();

            folder.ShowNewFolderButton = true;
            folder.RootFolder = Environment.SpecialFolder.MyComputer;

            var result = folder.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var path = folder.SelectedPath;
                var modinfo = System.IO.Path.Combine(path, ModManager.ModInfoFile);

                if (!Directory.Exists(path))
                {
                    this.ShowPopupMessage("Warning", "The selected folder does not exist. Could not open the mod package.");
                    return;
                }

                if(!File.Exists(modinfo))
                {
                    this.ShowPopupMessage("Warning", "The selected folder does not contain a mod.json file. Could not open the mod package.");
                    return;
                }


                var json = File.ReadAllText(modinfo);
                var mod = JsonSerializer.Deserialize<Mod>(json);
                
                if(mod == null)
                {
                    this.ShowPopupMessage("Warning", "Could not deserialize the mod.json file in the selected folder. Could not open mod package.");
                    return;
                }

                this.CurrentModContext.Mod = mod;
                this.CurrentModContext.Directory = path;
                this.Refresh();

            }

        }

        private void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            this.Save();
        }

        private void OnOpenDirectoryButtonClick(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = "explorer.exe",
                Arguments = this.CurrentModContext.Directory
            });
        }
    }
}
