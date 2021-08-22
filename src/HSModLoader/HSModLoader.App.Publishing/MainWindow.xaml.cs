using Ookii.Dialogs.WinForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
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

        private FileSystemWatcher FileWatcher { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            this.CurrentModContext = new ModContext();
            this.FileWatcher = new FileSystemWatcher();
            this.FileWatcher.Created += OnFilesChanged;
            this.FileWatcher.Changed += OnFilesChanged;
            this.FileWatcher.Renamed += OnFilesChanged;
            this.FileWatcher.Deleted += OnFilesChanged;

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
            Dispatcher.Invoke(() =>
            {
                this.ListViewFiles.Items.Clear();

                if (this.CurrentModContext.Mod == null)
                {
                    this.InfoPanel.IsEnabled = false;
                    this.FileListPanel.IsEnabled = false;
                    this.FileWatcher.EnableRaisingEvents = false;
                }
                else
                {
                    this.InfoPanel.IsEnabled = true;
                    this.FileListPanel.IsEnabled = true;
                    this.FileWatcher.EnableRaisingEvents = true;

                    var files = Directory.GetFiles(this.CurrentModContext.Directory).Select(x => new FileView(x));

                    foreach (var file in files)
                    {
                        this.ListViewFiles.Items.Add(file);
                    }

                }
            });
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
            var mod = this.CurrentModContext.Mod;
            if (mod != null)
            {
                var json = JsonSerializer.Serialize(mod, new JsonSerializerOptions() { WriteIndented = true });
                var path = System.IO.Path.Combine(this.CurrentModContext.Directory, Mod.InfoFile);
                File.WriteAllText(path, json);
            }
        }

        private void OnNewModButtonClick(object sender, RoutedEventArgs e)
        {
            this.ShowOverlay(true);

            var creation = new NewModWindow() { Owner = this };;
            var result = creation.ShowDialog();

            if(result == true)
            {
                this.SetCurrentMod(creation.ResultMod, creation.ResultDirectory);
                this.Save();
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
                var modinfo = System.IO.Path.Combine(path, Mod.InfoFile);

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

                this.SetCurrentMod(mod, path);
                
            }

        }

        private void SetCurrentMod(Mod mod, string directory)
        {
            this.CurrentModContext.Mod = mod;
            this.CurrentModContext.Directory = directory;
            this.FileWatcher.Path = directory;
            this.Refresh();
        }

        private void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            this.ShowOverlay(true);
            this.Save();
            this.ShowOverlay(false);
        }

        private void OnFilesChanged(object sender, FileSystemEventArgs e)
        {
            this.Refresh();
        }

        private void OnOpenDirectoryButtonClick(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = "explorer.exe",
                Arguments = this.CurrentModContext.Directory
            });
        }

        private void OnPublishButtonClick(object sender, RoutedEventArgs e)
        {

            this.ShowOverlay(true);

            try
            {
                var mod = this.CurrentModContext.Mod;

                if (mod == null)
                {
                    return;
                }

                if (string.IsNullOrEmpty(mod.Id))
                {
                    mod.Id = string.Format(
                        "{0}-{1}",
                        mod.Author.Replace(" ", string.Empty),
                        mod.Name.Replace(" ", string.Empty)
                    );
                    this.Save();
                }

                /*
                if (mod.DistributionType == RegistrationType.NotClassified)
                {
                    mod.DistributionType = RegistrationType.Standalone;
                    this.Save();
                }
                */

                var save = new VistaSaveFileDialog();
                save.CheckPathExists = true;
                save.OverwritePrompt = true;
                save.FileName = this.CurrentModContext.Mod.Id + ".hsmod";

                var result = save.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    var path = save.FileName;

                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }

                    ZipFile.CreateFromDirectory(this.CurrentModContext.Directory, path);
                }
            }
            catch(Exception ex)
            {
                this.ShowPopupMessage("Error", string.Format("Could not publish file: {0}", ex.Message));
            }

            this.ShowOverlay(false);

        }
    }
}
