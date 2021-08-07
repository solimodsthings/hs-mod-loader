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
        private List<ManagedMod> Mods;

        public MainWindow()
        {
            InitializeComponent();

            this.Mods = new List<ManagedMod>();
            this.ListAvailableMods.ItemsSource = Mods;

            // Special case for Superwolf mod which is part of base game install
            // Moves this to mods.json later
            var superwolf = new ManagedMod()
            {
                Name = "SuperWolf",
                Version = "1.0",
                Author = "Nathaniel3W",
                HasMutator = true,
                MutatorStartClass = "rpgtacgame.RPGTacMutator_SuperWolf"
            };

            this.Mods.Add(superwolf);
            this.ListAvailableMods.SelectedIndex = 0;

            UpdateOrderValue();
            UpdateInfoBox();

        }


        private void UpdateOrderValue()
        {
            int order = 1;
            foreach(var mod in Mods)
            {
                mod.Order = order++;
            }
        }

        private void UpdateInfoBox()
        {
            int selection = this.ListAvailableMods.SelectedIndex;

            if(selection >= 0)
            {
                var mod = this.Mods[selection];
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

        private void OnSelectedModChanged(object sender, SelectionChangedEventArgs e)
        {
            this.UpdateInfoBox();
        }
    }
}
