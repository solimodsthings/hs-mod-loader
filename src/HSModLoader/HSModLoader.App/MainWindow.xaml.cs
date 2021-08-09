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
            this.Manager = new ModManager();
            this.Manager.LoadFromFile();

            this.ModViews = new ObservableCollection<ModView>();
            this.ListAvailableMods.ItemsSource = this.ModViews;

            if(!Directory.Exists("mods"))
            {
                Directory.CreateDirectory("mods");
            }

            // Special case for SuperWolf mod which is part of base game install
            // Move this to mods.json later

            if (this.Manager.Mods.Count == 0)
            {
                this.Manager.RegisterMod(new Mod()
                {
                    Name = "SuperWolf",
                    Version = "1.0",
                    Author = "Nathaniel3W",
                    HasMutator = true,
                    MutatorStartClass = "rpgtacgame.RPGTacMutator_SuperWolf"
                });

            }

            this.RebuildModViews();

            this.SelectedMod = new ModView(this.Manager.Mods[0]);
            this.ModInfoPanel.DataContext = this.SelectedMod;
            this.ModStatePanel.DataContext = this.SelectedMod;

            this.ListAvailableMods.SelectedIndex = 0;

        }

        private void Save()
        {
            // TODO saving screen
            this.Manager.SaveToFile();
        }

        private void ShowGameFolderDialog()
        {
            this.CanvasFadeOut.Visibility = Visibility.Visible;
            var dialog = new GameFolderWindow(this.Manager);
            dialog.Owner = this;
            var result = dialog.ShowDialog();
            
            if(string.IsNullOrEmpty(this.Manager.GameFolderPath) && !(result.HasValue && result.Value))
            {
                this.Close();
            }
            else
            {
                this.Save();
            }

        }

        private void RebuildModViews()
        {
            this.ModViews.Clear();
            foreach (var mod in this.Manager.Mods)
            {
                this.ModViews.Add(new ModView(mod));
            }
        }

        private void OnSelectedModChanged(object sender, SelectionChangedEventArgs e)
        {
            int selection = this.ListAvailableMods.SelectedIndex;

            if (selection >= 0)
            {
                var mod = this.Manager.Mods[selection];
                this.SelectedMod.Set(mod);
            }
        }

        private void OnButtonSetGameFolderClick(object sender, RoutedEventArgs e)
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
                var cmod = this.Manager.Mods[selection];
                this.Manager.ShiftModOrderUp(selection);
                this.RebuildModViews();
                this.ListAvailableMods.SelectedIndex = cmod.OrderIndex;
            }

        }

        private void OnMoveModOrderDown(object sender, RoutedEventArgs e)
        {
            int selection = this.ListAvailableMods.SelectedIndex;

            if (selection < this.Manager.Mods.Count - 1)
            {
                var cmod = this.Manager.Mods[selection];
                this.Manager.ShiftModOrderDown(selection);
                this.RebuildModViews();
                this.ListAvailableMods.SelectedIndex = cmod.OrderIndex;
            }
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Save();
        }


    }
}
