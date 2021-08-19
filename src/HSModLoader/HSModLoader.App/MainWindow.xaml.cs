using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
            this.InitializeComponent();
            this.InitializeContextMenuComponent();

            if (!Directory.Exists("mods"))
            {
                Directory.CreateDirectory("mods");
            }

            this.Manager = new ModManager();
            this.Manager.Load();

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

        // The context menu for the main ListView is declared in XAML.
        // This method makes it so the context menu only appears if a ListViewItem
        // is right-licked, not anywhere in the ListView (including empty space).
        // If this can all be moved to XAML in the future, it should.
        private void InitializeContextMenuComponent()
        {
            var menu = this.ListAvailableMods.ContextMenu;
            var originalStyle = this.ListAvailableMods.ItemContainerStyle;

            var style = new Style();
            style.TargetType = typeof(ListViewItem);
            style.Setters.Add(new Setter(ListViewItem.ContextMenuProperty, menu));
            style.BasedOn = originalStyle;

            this.ListAvailableMods.ItemContainerStyle = style;
            this.ListAvailableMods.ContextMenu = null;
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
            this.Manager.Save();
        }

        /// <summary>
        /// Shows or hides a dark, transparent overlay across the entirety of the application window.
        /// </summary>
        /// <param name="show">True to show the overlay or false to turn it off.</param>
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
            else if (result == true && !string.IsNullOrEmpty(this.Manager.GameFolderPath))
            {
                this.Manager.InitializeGameModsFolder();
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
                this.SelectedMod.Set(new ModConfiguration());
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
                        var result = this.Manager.RegisterMod(filepath);
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
                    var result = this.Manager.RegisterMod(browse.FileName);
                    this.HandleRegistrationResult(result);
                }
            }
        }

        private void OnRemoveMod(object sender, RoutedEventArgs e)
        {
            var selectedIndex = this.ListAvailableMods.SelectedIndex;

            if(selectedIndex >= 0 && selectedIndex < this.Manager.ModConfigurations.Count)
            {
                this.ShowOverlay(true);
                
                var configuration = this.Manager.ModConfigurations[selectedIndex];
                var mod = configuration.Mod;
                var message = string.Format("Are you sure you want to uninstall and permanently remove mod '{0}' version {1}?", mod.Name, mod.Version);
                
                var confirmation = new ConfirmationWindow("Confirmation", message);
                confirmation.Owner = this;

                var result = confirmation.ShowDialog();

                if(result == true)
                {
                    this.Manager.UnregisterMod(configuration);
                    this.Manager.Save();
                    this.RebuildModViews();
                    this.ListAvailableMods.SelectedIndex = -1;
                }

                this.ShowOverlay(false);
            }

        }

        private void HandleRegistrationResult(Result result)
        {
            if (result.IsSuccessful)
            {
                this.RebuildModViews();
                this.ListAvailableMods.SelectedIndex = this.Manager.ModConfigurations.Count - 1;
            }
            else
            {
                this.ShowPopupMessage("Warning", result.ErrorMessage);
            }
        }

        private void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            this.ShowOverlay(true);
            this.ShowProgressOverlay(true);

            var worker = new BackgroundWorker();
            worker.DoWork += ApplyModsAsync;
            worker.RunWorkerCompleted += delegate (object s, RunWorkerCompletedEventArgs args)
            {
                Dispatcher.Invoke(() => {
                    this.ShowOverlay(false);
                    this.ShowProgressOverlay(false);
                });
            };
            worker.RunWorkerAsync();
        }

        private void ApplyModsAsync(object sender, DoWorkEventArgs e)
        {
            var start = DateTime.Now;

            this.Manager.Save();
            var result = this.Manager.ApplyMods();

            if (!result.IsSuccessful)
            {
                Dispatcher.Invoke(() =>
                {
                    this.ShowPopupMessage("Warning", result.ErrorMessage + "  See error.log for more details.");
                    this.Manager.Save();
                    this.RebuildModViews();
                    this.SelectedMod.Refresh();
                });
            }
            else
            {
                // This gives user some visual feedback that something actually happened
                // if applying mods occurred too quickly to show a loading animation
                var duration = (DateTime.Now - start).TotalSeconds;
                if (duration < 0.5)
                {
                    Thread.Sleep((int)(0.5 * 1000) - (int)(duration * 1000));
                }
            }
        }


        private void OnLaunchGameButtonClick(object sender, RoutedEventArgs e)
        {
            Process.Start(this.Manager.GetPathToGameExecutable());
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Save();
        }

        private void OnRightClickMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var index = this.ListAvailableMods.SelectedIndex;

            if(index >= 0 && index < this.Manager.ModConfigurations.Count)
            {
                var configuration = this.Manager.ModConfigurations[index];
                
                this.MenuItemEnableMod.IsEnabled = true;
                this.MenuItemSoftDisableMod.IsEnabled = true;
                this.MenuItemDisableMod.IsEnabled = true;

                if (configuration.State == ModState.Enabled)
                {
                    this.MenuItemEnableMod.IsEnabled = false;
                }
                if (configuration.State == ModState.SoftDisabled)
                {
                    this.MenuItemSoftDisableMod.IsEnabled = false;
                }
                if (configuration.State == ModState.Disabled)
                {
                    this.MenuItemDisableMod.IsEnabled = false;
                }
                
            }
            else
            {
                // e.Handled = true;
            }
        }

        private void OnMenuItemEnableMod(object sender, RoutedEventArgs e)
        {
            var index = this.ListAvailableMods.SelectedIndex;

            if (index >= 0 && index < this.Manager.ModConfigurations.Count)
            {
                var configuration = this.Manager.ModConfigurations[index];
                configuration.State = ModState.Enabled;
                this.ListAvailableMods.Items.Refresh();
                this.SelectedMod.Refresh();
            }
        }

        private void OnMenuItemSoftDisableMod(object sender, RoutedEventArgs e)
        {
            var index = this.ListAvailableMods.SelectedIndex;

            if (index >= 0 && index < this.Manager.ModConfigurations.Count)
            {
                var configuration = this.Manager.ModConfigurations[index];
                configuration.State = ModState.SoftDisabled;
                this.ListAvailableMods.Items.Refresh();
                this.SelectedMod.Refresh();
            }
        }

        private void OnMenuItemDisableMod(object sender, RoutedEventArgs e)
        {
            var index = this.ListAvailableMods.SelectedIndex;

            if (index >= 0 && index < this.Manager.ModConfigurations.Count)
            {
                var configuration = this.Manager.ModConfigurations[index];
                configuration.State = ModState.Disabled;
                this.ListAvailableMods.Items.Refresh();
                this.SelectedMod.Refresh();
            }
        }


    }
}
