using Ookii.Dialogs.WinForms;
using Steamworks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Text.Json;
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

namespace HSModLoader.App.Publishing
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private ModContext CurrentModContext { get; set; }
        private FileSystemWatcher FileWatcher { get; set; }
        private BackgroundWorker BackgroundWorker { get; set;}
        private ulong UploadItemId { get; set; }
        private bool SubmissionFinished { get; set; }
        private bool SubmissionSuccessful { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            this.CurrentModContext = new ModContext();
            this.FileWatcher = new FileSystemWatcher();
            this.FileWatcher.Created += OnFilesChanged;
            this.FileWatcher.Changed += OnFilesChanged;
            this.FileWatcher.Renamed += OnFilesChanged;
            this.FileWatcher.Deleted += OnFilesChanged;

            this.BackgroundWorker = new BackgroundWorker();
            this.BackgroundWorker.DoWork += PublishSteamMod;
            this.BackgroundWorker.RunWorkerCompleted += OnPublishSteamModComplete;
            this.BackgroundWorker.WorkerSupportsCancellation = true;

            this.DataContext = this.CurrentModContext;
        }


        private void ShowOverlay(bool show)
        {
            Dispatcher.Invoke(() =>
            {
                if (show)
                {
                    this.CanvasFadeOut.Visibility = Visibility.Visible;
                }
                else
                {
                    this.CanvasFadeOut.Visibility = Visibility.Collapsed;
                }
            });
            
        }
        private void ShowProgressOverlay(bool show)
        {
            Dispatcher.Invoke(() =>
            {
                if (show)
                {
                    this.ProgressRing.Visibility = Visibility.Visible;
                }
                else
                {
                    this.ProgressRing.Visibility = Visibility.Collapsed;
                }
            });
        }

        private void RefreshFiles()
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

        private void ShowPopupMessage(string header, string body, bool useOverlay = true)
        {

            Dispatcher.Invoke(() =>
            {
                if (useOverlay)
                {
                    this.ShowOverlay(true);
                }


                var dialog = new MessageWindow(header, body);
                dialog.Owner = this;

                dialog.ShowDialog();

                if (useOverlay)
                {
                    this.ShowOverlay(false);
                }
            });
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
            this.RefreshFiles();
        }

        private void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            this.ShowOverlay(true);
            this.Save();
            this.ShowOverlay(false);
        }

        private void OnFilesChanged(object sender, FileSystemEventArgs e)
        {
            this.RefreshFiles();
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

            if (this.CurrentModContext.Mod == null)
            {
                return;
            }

            this.ShowOverlay(true);
            var publish = new PublishingWindow() { Owner = this };
            var result = publish.ShowDialog();

            if (result == true)
            {
                
                var selection = publish.SelectedPublishingMode;

                if(selection == PublishingMode.Standalone)
                {
                    this.ShowProgressOverlay(true);

                    this.PublishStandaloneMod();

                    this.ShowProgressMessage(string.Empty);
                    this.ShowProgressOverlay(false);
                    this.ShowOverlay(false);
                }
                else
                {
                    this.ShowProgressOverlay(true);
                    
                    while(this.BackgroundWorker.IsBusy)
                    {
                        this.BackgroundWorker.CancelAsync();
                    }

                    this.BackgroundWorker.RunWorkerAsync();
                }
            }
            else
            {
                this.ShowProgressMessage(string.Empty);
                this.ShowProgressOverlay(false);
                this.ShowOverlay(false);
            }
        }

        private void PublishStandaloneMod()
        {

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

                var save = new VistaSaveFileDialog();
                save.InitialDirectory = new DirectoryInfo(this.CurrentModContext.Directory).Parent.FullName;
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
            catch (Exception ex)
            {
                this.ShowPopupMessage("Error", string.Format("Could not publish file: {0}", ex.Message));
            }

        }

        private void PublishSteamMod(object sender, DoWorkEventArgs e)
        {
            try
            {
                var mod = this.CurrentModContext.Mod;

                bool isNewItem = false;
                this.UploadItemId = 0;
                this.SubmissionFinished = false;
                this.SubmissionSuccessful = false;

                if (mod.SteamWorkshopId.HasValue)
                {
                    this.UploadItemId = mod.SteamWorkshopId.Value;
                }

                this.ShowProgressMessage("Accessing Steam");

                if (SteamAPI.Init())
                {
                    var appId = new AppId_t((uint)669500);

                    if (UploadItemId == 0)
                    {

                        this.ShowProgressMessage("Creating new Steam Workshop item");

                        var createItemCall = SteamUGC.CreateItem(appId, EWorkshopFileType.k_EWorkshopFileTypeCommunity);
                        var createResult = CallResult<CreateItemResult_t>.Create(OnSteamWorkshopItemCreation);
                        createResult.Set(createItemCall);

                        while (UploadItemId == 0)
                        {
                            SteamAPI.RunCallbacks();
                            Thread.Sleep(1000);
                        }

                        this.CurrentModContext.Mod.SteamWorkshopId = UploadItemId;
                        this.CurrentModContext.SteamId = UploadItemId.ToString();
                        isNewItem = true;
                    }

                    this.ShowProgressMessage("Updating Steam Workshop item");

                    var updateHandle = SteamUGC.StartItemUpdate(appId, new PublishedFileId_t(UploadItemId));

                    if(isNewItem)
                    {
                        SteamUGC.SetItemTitle(updateHandle, mod.Name);
                        SteamUGC.SetItemDescription(updateHandle, mod.Description);
                        SteamUGC.SetItemVisibility(updateHandle, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic);
                    }
                    
                    SteamUGC.SetItemContent(updateHandle, this.CurrentModContext.Directory);

                    var submitItemCall = SteamUGC.SubmitItemUpdate(updateHandle, "Updated on " + DateTime.Now.ToString());
                    var submitResult = CallResult<SubmitItemUpdateResult_t>.Create(OnSteamWorkshopItemSubmission);
                    submitResult.Set(submitItemCall);

                    this.ShowProgressMessage("Uploading mod files");

                    while (!SubmissionFinished)
                    {
                        SteamAPI.RunCallbacks();

                        // ulong bytesDone;
                        // ulong bytesTotal;
                        // EItemUpdateStatus status = SteamUGC.GetItemUpdateProgress(updateHandle, out bytesDone, out bytesTotal);

                        Thread.Sleep(1000);
                    }

                    SteamAPI.Shutdown();
                    this.ShowProgressMessage(string.Empty);

                    Dispatcher.Invoke(() =>
                    {
                        this.Save();
                        this.ShowProgressOverlay(false);

                        if(this.SubmissionSuccessful)
                        {
                            if (isNewItem)
                            {
                                this.ShowPopupMessage("Success", "Successfully uploaded your mod as a new Steam Workshop item!"
                                    + " Visit your new workshop item in Steam to add screenshots.", false);
                            }
                            else
                            {
                                this.ShowPopupMessage("Success", "Successfully updated the Steam Workshop item for this mod!"
                                    + " Visit your workshop item in Steam to edit your change notes.", false);
                            }
                        }
                    });
                    
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    this.ShowPopupMessage("Error", ex.Message, false);
                });

                SteamAPI.Shutdown();
            }
        }

        private void ShowProgressMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                this.TextboxProgress.Text = message;
                
                if (string.IsNullOrEmpty(message))
                {
                    this.TextboxProgress.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.TextboxProgress.Visibility = Visibility.Visible;
                }
            });
        }

        private void OnPublishSteamModComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                this.ShowProgressMessage(string.Empty);
                this.ShowProgressOverlay(false);
                this.ShowOverlay(false);
            });
        }

        private void OnSteamWorkshopItemCreation(CreateItemResult_t result, bool failure)
        {
            if (result.m_eResult == EResult.k_EResultOK)
            {
                this.UploadItemId = ulong.Parse(result.m_nPublishedFileId.ToString());
            }
            else
            {
                this.ShowPopupMessage("Error", "Failed to create a new Steam Workshop item for your mod.", false);
                this.OnPublishSteamModComplete(null, null);
            }
        }

        private void OnSteamWorkshopItemSubmission(SubmitItemUpdateResult_t result, bool failure)
        {

            this.SubmissionFinished = true;

            if (result.m_eResult == EResult.k_EResultOK)
            {
                this.SubmissionSuccessful = true;
            }
            else
            {
                this.ShowPopupMessage("Error", 
                    string.Format("Failed to upload mod files to the Steam Workshop. There either does not exist a workshop item with ID {0} or you don't have permissions to update it.", this.CurrentModContext.SteamId), false);
                this.OnPublishSteamModComplete(null, null);
            }
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            SteamAPI.Shutdown();

            while(this.BackgroundWorker.IsBusy)
            {
                if (!this.BackgroundWorker.CancellationPending)
                {
                    this.BackgroundWorker.CancelAsync();
                }

                Thread.Sleep(1000);
            }
        }
    }
}
