using HSModLoader.WpfControls;
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

        private static readonly string DefaultAppIdFile = "steam_appid.txt";
        private static readonly int DefaultAppId = 669500;

        private ModContext ModContext { get; set; }
        private FileSystemWatcher ModDirectoryWatcher { get; set; }
        private BackgroundWorker SteamApiThread { get; set;}
        private ulong SteamUploadItemId { get; set; }
        private bool SteamSubmissionFinished { get; set; }
        private bool SteamSubmissionSuccessful { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            this.ModContext = new ModContext();
            this.ModDirectoryWatcher = new FileSystemWatcher();
            this.ModDirectoryWatcher.Created += OnFilesChanged;
            this.ModDirectoryWatcher.Changed += OnFilesChanged;
            this.ModDirectoryWatcher.Renamed += OnFilesChanged;
            this.ModDirectoryWatcher.Deleted += OnFilesChanged;

            this.SteamApiThread = new BackgroundWorker();
            this.SteamApiThread.DoWork += PublishSteamMod;
            this.SteamApiThread.RunWorkerCompleted += OnPublishSteamModComplete;
            this.SteamApiThread.WorkerSupportsCancellation = true;

            this.DataContext = this.ModContext;
        }

        #region Overlay
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
        #endregion

        private void RefreshFiles()
        {
            Dispatcher.Invoke(() =>
            {
                this.ListViewFiles.Items.Clear();

                if (this.ModContext.Mod == null)
                {
                    this.InfoPanel.IsEnabled = false;
                    this.FileListPanel.IsEnabled = false;
                    this.ModDirectoryWatcher.EnableRaisingEvents = false;
                }
                else
                {
                    this.InfoPanel.IsEnabled = true;
                    this.FileListPanel.IsEnabled = true;
                    this.ModDirectoryWatcher.EnableRaisingEvents = true;

                    var files = Directory.GetFiles(this.ModContext.Directory).Select(x => new FileView(x));

                    foreach (var file in files)
                    {
                        this.ListViewFiles.Items.Add(file);
                    }

                }
            });
        }

        public void ShowPopupMessage(string header, string body, bool useOverlay = true)
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
            var mod = this.ModContext.Mod;
            if (mod != null)
            {
                var json = JsonSerializer.Serialize(mod, new JsonSerializerOptions() { WriteIndented = true });
                var path = System.IO.Path.Combine(this.ModContext.Directory, Mod.InfoFile);
                File.WriteAllText(path, json);
            }
        }

        private void OnNewModButtonClick(object sender, RoutedEventArgs e)
        {
            this.ShowOverlay(true);

            var creation = new NewModWindow() { Owner = this };
            var result = creation.ShowDialog();
            var noEncounteredIssues = true;

            if(result == true)
            {

                if (!Directory.Exists(creation.ResultModParentFolder))
                {
                    this.ShowPopupMessage("Warning", "Cannot create mod. The specified parent folder where the mod is to be created inside does not exist.");
                    noEncounteredIssues = false;
                }

                var modFolder = System.IO.Path.Combine(creation.ResultModParentFolder, creation.ResultMod.Name);

                if(noEncounteredIssues)
                {
                    if (Directory.Exists(modFolder))
                    {
                        this.ShowPopupMessage("Warning", "Cannot create mod. The desired combination of mod name and parent directory is already used by an existing mod.");
                        noEncounteredIssues = false;
                    }
                    else
                    {
                        Directory.CreateDirectory(modFolder);
                    }
                }

                if(noEncounteredIssues)
                {
                    this.SetCurrentMod(creation.ResultMod, modFolder);
                    this.Save();
                }
                
            }

            this.ShowOverlay(false);
        }

        private void OnOpenModButtonClick(object sender, RoutedEventArgs e)
        {
            if(this.ModContext.Mod != null)
            {
                // TODO Prompt user if they actually want to save before opening another mod package folder
                this.Save();
            }

            var folder = new VistaFolderBrowserDialog();

            folder.RootFolder = Environment.SpecialFolder.Desktop;
            folder.SelectedPath = Directory.GetCurrentDirectory() + "\\"; // The extra backslash makes it is so we start inside the folder
            folder.ShowNewFolderButton = true;

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

                this.ShowOverlay(true);
                this.ShowProgressOverlay(true);

                var json = File.ReadAllText(modinfo);
                var mod = JsonSerializer.Deserialize<Mod>(json);
                
                if(mod == null)
                {
                    this.ShowPopupMessage("Warning", "Could not deserialize the mod.json file in the selected folder. Could not open mod package.");
                }
                else
                {
                    this.SetCurrentMod(mod, path);
                }

                

                this.ShowProgressOverlay(false);
                this.ShowOverlay(false);
                

            }

        }

        private void SetCurrentMod(Mod mod, string directory)
        {
            this.ModContext.Mod = mod;
            this.ModContext.Directory = directory;
            this.ModDirectoryWatcher.Path = directory;
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
                Arguments = this.ModContext.Directory
            });
        }

        private void OnPublishButtonClick(object sender, RoutedEventArgs e)
        {
            
            if (this.ModContext.Mod == null)
            {
                this.ShowPopupMessage("Warning", "There is no active mod to publish. Please open a mod first.");
                return;
            }

            var mod = this.ModContext.Mod;

            // The constraint on Description is a requirement for Steam only, but we might
            // as well enforce it for standalone mods too
            if (string.IsNullOrEmpty(this.ModContext.Name) || string.IsNullOrEmpty(this.ModContext.Description))
            {
                this.ShowPopupMessage("Warning", "Cannot publish a mod with a missing name or description.");
                return;
            }

            if (string.IsNullOrEmpty(this.ModContext.Author) || string.IsNullOrEmpty(this.ModContext.Description))
            {
                this.ShowPopupMessage("Warning", "You must provide an author's name for this mod before publishing.");
                return;
            }

            this.ShowOverlay(true);

            if (string.IsNullOrEmpty(mod.Id))
            {
                this.ModContext.ModId = string.Format(
                    "{0}-{1}",
                    mod.Author.Replace(" ", string.Empty),
                    mod.Name.Replace(" ", string.Empty)
                );

                this.Save();
            }

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

                    // It's a requirement for the Steam API that this
                    // file exists in the application directory
                    if (!File.Exists(DefaultAppIdFile))
                    {
                        File.WriteAllText(DefaultAppIdFile, DefaultAppId.ToString());
                    }

                    while(this.SteamApiThread.IsBusy)
                    {
                        if(!this.SteamApiThread.CancellationPending)
                        {
                            this.SteamApiThread.CancelAsync();
                        }
                        Thread.Sleep(1000);
                    }

                    this.SteamApiThread.RunWorkerAsync();
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
                var mod = this.ModContext.Mod;

                if (mod == null)
                {
                    return;
                }

                var save = new VistaSaveFileDialog();
                save.InitialDirectory = Directory.GetCurrentDirectory() + "\\";
                save.CheckPathExists = true;
                save.OverwritePrompt = true;
                save.FileName = this.ModContext.Mod.Id + ".hsmod";

                var result = save.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    var path = save.FileName;

                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }

                    ZipFile.CreateFromDirectory(this.ModContext.Directory, path);
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
                var mod = this.ModContext.Mod;
                var isExistingSteamWorkshopItem = mod.SteamWorkshopId.HasValue;

                this.SteamUploadItemId = 0;
                this.SteamSubmissionFinished = false;
                this.SteamSubmissionSuccessful = false;

                if (isExistingSteamWorkshopItem)
                {
                    this.SteamUploadItemId = mod.SteamWorkshopId.Value;
                }

                this.ShowProgressMessage("Accessing Steam");
                
                if (SteamAPI.Init())
                {
                    var appId = new AppId_t((uint)DefaultAppId);

                    if (!isExistingSteamWorkshopItem)
                    {

                        this.ShowProgressMessage("Creating new Steam Workshop item");

                        var createItemCall = SteamUGC.CreateItem(appId, EWorkshopFileType.k_EWorkshopFileTypeCommunity);
                        var createResult = CallResult<CreateItemResult_t>.Create(OnSteamWorkshopItemCreation);
                        createResult.Set(createItemCall);

                        while (SteamUploadItemId == 0 && !this.SteamApiThread.CancellationPending)
                        {
                            SteamAPI.RunCallbacks();
                            Thread.Sleep(1000);
                        }

                        this.ModContext.Mod.SteamWorkshopId = SteamUploadItemId;
                        this.ModContext.SteamId = SteamUploadItemId.ToString();
                        this.Save();
                    }

                    this.ShowProgressMessage("Updating Steam Workshop item");

                    var updateHandle = SteamUGC.StartItemUpdate(appId, new PublishedFileId_t(SteamUploadItemId));

                    if(!isExistingSteamWorkshopItem)
                    {
                        SteamUGC.SetItemTitle(updateHandle, mod.Name);
                        SteamUGC.SetItemDescription(updateHandle, mod.Description);
                        SteamUGC.SetItemVisibility(updateHandle, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic);
                    }

                    var thumbnail = System.IO.Path.Combine(this.ModContext.Directory, "thumbnail.jpg");

                    if (File.Exists(thumbnail)){
                        SteamUGC.SetItemPreview(updateHandle, thumbnail);
                    }
                    
                    SteamUGC.SetItemContent(updateHandle, this.ModContext.Directory);

                    var submitItemCall = SteamUGC.SubmitItemUpdate(updateHandle, "Updated on " + DateTime.Now.ToString());
                    var submitResult = CallResult<SubmitItemUpdateResult_t>.Create(OnSteamWorkshopItemSubmission);
                    submitResult.Set(submitItemCall);

                    this.ShowProgressMessage("Uploading mod files");

                    while (!SteamSubmissionFinished && !this.SteamApiThread.CancellationPending)
                    {
                        SteamAPI.RunCallbacks();

                        // ulong bytesDone;
                        // ulong bytesTotal;
                        // EItemUpdateStatus status = SteamUGC.GetItemUpdateProgress(updateHandle, out bytesDone, out bytesTotal);

                        Thread.Sleep(1000);
                    }

                    SteamAPI.Shutdown();
                    this.Save();

                    Dispatcher.Invoke(() =>
                    {
                        this.ShowProgressMessage(string.Empty);
                        this.ShowProgressOverlay(false);

                        if(this.SteamSubmissionSuccessful)
                        {
                            if (isExistingSteamWorkshopItem)
                            {
                                this.ShowPopupMessage("Success", "Successfully updated the Steam Workshop item for this mod!"
                                    + " Visit your workshop item in Steam to edit your change notes.", false);
                            }
                            else
                            {
                                this.ShowPopupMessage("Success", "Successfully uploaded your mod as a new Steam Workshop item!"
                                    + " Visit your new workshop item in Steam to add screenshots.", false);
                            }
                        }
                    });
                    
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        this.ShowProgressOverlay(false);
                        this.ShowPopupMessage("Warning", "Steam must be running in order to publish mods to the Steam Workshop.");
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    this.ShowProgressOverlay(false);
                    this.ShowPopupMessage("Error", ex.Message, false);
                });

                SteamAPI.Shutdown();
            }
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
                this.SteamUploadItemId = ulong.Parse(result.m_nPublishedFileId.ToString());
            }
            else
            {
                this.ShowPopupMessage("Error", "Failed to create a new Steam Workshop item for your mod.", false);
                this.OnPublishSteamModComplete(null, null);
            }
        }

        private void OnSteamWorkshopItemSubmission(SubmitItemUpdateResult_t result, bool failure)
        {

            this.SteamSubmissionFinished = true;

            if (result.m_eResult == EResult.k_EResultOK)
            {
                this.SteamSubmissionSuccessful = true;
            }
            else
            {
                this.ShowPopupMessage("Error", 
                    string.Format("Failed to upload mod files to the Steam Workshop. There either does not exist a workshop item with ID {0} or you don't have permissions to update it.", this.ModContext.SteamId), false);
                this.OnPublishSteamModComplete(null, null);
            }
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            SteamAPI.Shutdown();

            while(this.SteamApiThread.IsBusy)
            {

                if (!this.SteamApiThread.CancellationPending)
                {
                    this.SteamApiThread.CancelAsync();
                }

                Thread.Sleep(1000);
            }
        }
    }
}
