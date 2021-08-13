using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;
using HSModLoader;
using Microsoft.Win32;

namespace HSModLoader.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ModManager Manager { get; set; }

        public ObservableCollection<ModView> ModViews { get; set; }
        public ModView SelectedMod { get; set; }
        
        public MainWindow()
        {
            InitializeComponent();

            if (!Directory.Exists("mods"))
            {
                Directory.CreateDirectory("mods");
            }

            this.Manager = new ModManager();
            this.Manager.LoadFromFile();

            this.ModViews = new ObservableCollection<ModView>();
            this.ListAvailableMods.ItemsSource = this.ModViews;
            this.RebuildModViews();

            this.SelectedMod = new ModView(new ModConfiguration());
            this.ModInfoPanel.DataContext = this.SelectedMod;
            this.ModStatePanel.DataContext = this.SelectedMod;

            if(this.Manager.ModConfigurations.Count > 0)
            {
                this.ListAvailableMods.SelectedIndex = 0;
            }

        }

        private void RebuildModViews()
        {
            this.ModViews.Clear();
            foreach (var mod in this.Manager.ModConfigurations)
            {
                this.ModViews.Add(new ModView(mod));
            }
        }

        // This is a method in case we need to put an task-in-progress animation
        // when data is being serialized to disk
        private void Save()
        {
            this.Manager.SaveToFile();
        }

        /// <summary>
        /// Shows or hides a dark, transparent overlay across the entirety of the application window.
        /// </summary>
        /// <param name="show">True to show the overlay or false to turn it off.</param>
        private void ShowOverlay(bool show)
        {
            if(show)
            {
                this.CanvasFadeOut.Visibility = Visibility.Visible;
            }
            else
            {
                this.CanvasFadeOut.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowGameFolderDialog()
        {
            this.ShowOverlay(true);

            var dialog = new GameFolderWindow(this.Manager);
            dialog.Owner = this;

            var result = dialog.ShowDialog();

            // Exit the application if the user doesn't want
            // to provide the game folder path and one hasn't been
            // defined yet (basically a first-time use scenario)
            if (result != true && string.IsNullOrEmpty(this.Manager.GameFolderPath))
            {
                this.Close();
            }
            else
            {
                this.Save();
            }

            this.ShowOverlay(false);

        }

        private void ShowPopupMessage(string header, string body)
        {
            this.ShowOverlay(true);
            var dialog = new MessageWindow(header, body);
            dialog.Owner = this;

            dialog.ShowDialog();

            this.ShowOverlay(false);

        }

        private void OnSelectedModChanged(object sender, SelectionChangedEventArgs e)
        {
            int selection = this.ListAvailableMods.SelectedIndex;

            if (selection >= 0 && selection < this.Manager.ModConfigurations.Count)
            {
                var mod = this.Manager.ModConfigurations[selection];
                this.SelectedMod.Set(mod);
                this.InfoPanel.IsEnabled = true;
            }
            else
            {
                this.InfoPanel.IsEnabled = false;
            }
        }

        private void OnSetGameFolderButtonClick(object sender, RoutedEventArgs e)
        {
            this.ShowGameFolderDialog();
        }

        private void OnWindowContentRendered(object sender, EventArgs e)
        {
            if(string.IsNullOrEmpty(this.Manager.GameFolderPath))
            {
                this.ShowGameFolderDialog();
            }
        }

        private void OnControllerUsed(object sender, RoutedEventArgs e)
        {
            this.ListAvailableMods.Items.Refresh();
        }

        private void OnMoveModOrderUp(object sender, RoutedEventArgs e)
        {
            int selection = this.ListAvailableMods.SelectedIndex;

            if (selection > 0)
            {
                var configuration = this.Manager.ModConfigurations[selection];
                this.Manager.ShiftModOrderUp(selection);
                this.RebuildModViews();
                this.ListAvailableMods.SelectedIndex = configuration.OrderIndex;
            }

        }

        private void OnMoveModOrderDown(object sender, RoutedEventArgs e)
        {
            int selection = this.ListAvailableMods.SelectedIndex;

            if (selection < this.Manager.ModConfigurations.Count - 1)
            {
                var configuration = this.Manager.ModConfigurations[selection];
                this.Manager.ShiftModOrderDown(selection);
                this.RebuildModViews();
                this.ListAvailableMods.SelectedIndex = configuration.OrderIndex;
            }
        }

        private void OnAddNewModByDragDrop(object sender, DragEventArgs e)
        {

            var files = e.Data.GetData(DataFormats.FileDrop) as string[];

            if(files != null)
            {
                var v = new Validator();
                foreach (var filepath in files)
                {
                    if (File.Exists(filepath) && v.IsModPackage(filepath))
                    {
                        var result = this.Manager.RegisterModFromFile(filepath);
                        this.HandleRegistrationResult(result);
                    }
                }
            }

        }

        private void OnAddNewModButtonClick(object sender, RoutedEventArgs e)
        {
            var browse = new OpenFileDialog();
            browse.CheckFileExists = true;
            browse.DefaultExt = ".hsmod"; // Default file extension
            browse.Filter = "Himeko Sutori Mod (.hsmod)|*.hsmod"; // Filter files by extension

            if (browse.ShowDialog() == true)
            {
                var v = new Validator();
                if (File.Exists(browse.FileName) && v.IsModPackage(browse.FileName))
                {
                    var result = this.Manager.RegisterModFromFile(browse.FileName);
                    this.HandleRegistrationResult(result);
                }
            }
        }

        private void HandleRegistrationResult(Result result)
        {
            if (result.Value)
            {
                this.RebuildModViews();
                this.ListAvailableMods.SelectedIndex = this.Manager.ModConfigurations.Count - 1;
            }
            else
            {
                this.ShowPopupMessage("Warning", result.Message);
            }
        }

        private void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            this.ShowOverlay(true);
            // TODO: show an in-progress animation
            this.Manager.ApplyMods();
            this.ShowOverlay(false);
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Save();
        }

    }
}
