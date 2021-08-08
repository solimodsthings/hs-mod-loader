using System;
using System.Collections.Generic;
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
        private ModManager Manager;
        
        public MainWindow()
        {
            InitializeComponent();
            this.Manager = new ModManager();
            this.Manager.LoadFromFile();
        
            this.ListAvailableMods.ItemsSource = this.Manager.Mods;

            // Special case for Superwolf mod which is part of base game install
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

            this.ListAvailableMods.SelectedIndex = 0;
            this.UpdateModInfoBox();

        }

        private void Save()
        {
            // TODO saving screen
            this.Manager.SaveToFile();
        }

        private void UpdateModInfoBox()
        {
            int selection = this.ListAvailableMods.SelectedIndex;

            if(selection >= 0)
            {
                var mod = this.Manager.Mods[selection];
                this.LabelModName.Content = mod.Name;
                this.LabelModVersion.Content = mod.Version;
                this.LabelAuthorName.Content = mod.Author;
                this.LabelModSourceUrl.Content = mod.OptionalUrl;

                // TODO: use binding instead
                this.RadioButtonIsDisabled.IsChecked = (mod.State == ModState.Disabled);
                this.RadioButtonIsEnabled.IsChecked = (mod.State == ModState.Enabled);
                this.RadioButtonIsSoftDisabled.IsChecked = (mod.State == ModState.SoftDisabled);

            }
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
        private void OnSelectedModChanged(object sender, SelectionChangedEventArgs e)
        {
            this.UpdateModInfoBox();
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

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Save();
        }
    }
}
