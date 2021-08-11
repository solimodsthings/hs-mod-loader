using Microsoft.Win32;
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

namespace HSModLoader.App
{
    /// <summary>
    /// This is a dialog window that allows a user to set the
    /// path to the game.
    /// </summary>
    public partial class GameFolderWindow : Window
    {
        private ModManager Manager;
        private Validator Validator;

        public GameFolderWindow(ModManager manager)
        {
            InitializeComponent();

            this.Manager = manager;
            this.Validator = new Validator();

            if(!string.IsNullOrEmpty(Manager.GameFolderPath))
            {
                this.TextBoxGameFolderPath.Text = this.Manager.GameFolderPath;
                this.ButtonCancelGameFolder.Visibility = Visibility.Visible;
            }
        }

        private void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            var folder = this.TextBoxGameFolderPath.Text;

            if (this.Validator.IsGameFolder(folder))
            {
                this.Manager.GameFolderPath = folder;
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                // Check to see if the user selected a subdirectory of the main game
                // folder. If they did then it is posisble to extract the correct path.

                var possibleMatch = this.Validator.CheckIfParentIsGameFolder(folder, 3);
                if(!string.IsNullOrEmpty(possibleMatch) && this.Validator.IsGameFolder(possibleMatch))
                {
                    this.Manager.GameFolderPath = possibleMatch;
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    this.TextBlockErrorMessage.Visibility = Visibility.Visible;
                }
            }
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

    }
}
