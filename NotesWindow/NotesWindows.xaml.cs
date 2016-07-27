using System;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Threading;

namespace SteveHall.NotesWindow
{
    /// <summary>
    /// Interaction logic for NotesWindow.xaml
    /// </summary>
    public partial class NotesWindow : System.Windows.Controls.UserControl, IDisposable
    {
        private bool isInitialized;
        private bool isAutoSave;

        private FileSystemWatcher watcher;
        private bool isUpdatingSelf;
        private bool isReloadRequired;

        private static string DATA_SOURCE = "notes";

        public NotesWindow()
        {
            InitializeComponent();

            this.Loaded += NotesWindow_Loaded;
            this.Unloaded += NotesWindow_Unloaded;

            Initialize();
        }

        private void Initialize()
        {
            if (!this.isInitialized)
            {
                this.NotesTextBox.IsReadOnly = true;

                IsolatedStorageFile f = IsolatedStorageFile.GetUserStoreForAssembly();
                IsolatedStorageFileStream stream = null;
                try
                {
                    // using try/finally rather than using to avoid CA2202 warning (https://msdn.microsoft.com/en-us/library/ms182334.aspx)
                    stream = new IsolatedStorageFileStream(DATA_SOURCE, FileMode.OpenOrCreate, f);

                    if (f.FileExists(DATA_SOURCE))
                    {
                        try
                        {
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                stream = null; // ditto; avoids CA2202

                                string fileLocation = reader.ReadLine();
                                if (!bool.TryParse(reader.ReadLine(), out this.isAutoSave))
                                    this.isAutoSave = false;

                                SetAutoSaveUI();

                                if (ReadFile(fileLocation, false))
                                {
                                    CreateFileSystemWatcher(fileLocation);

                                    this.FilePathTextBox.Text = fileLocation;
                                    this.NotesTextBox.IsReadOnly = false;
                                }
                                else
                                {
                                    this.FilePathTextBox.Text = string.Empty;
                                    this.NotesTextBox.IsReadOnly = true;
                                }

                                this.isInitialized = true;
                            }
                        }
                        catch (Exception x)
                        {
                            this.NotesTextBox.TextChanged -= Notes_TextChanged;
                            this.NotesTextBox.Text = "Failed to identify previous file location: " + x.Message;
                            this.NotesTextBox.TextChanged += Notes_TextChanged;
                        }
                    }
                }
                finally
                {
                    if (stream != null)
                        stream.Dispose();
                }
            }
        }

        private bool ReadFile(string fileLocation, bool persistPath)
        {
            if (!string.IsNullOrEmpty(fileLocation) && File.Exists(fileLocation))
            {
                try
                {
                    FileStream inStream = null;
                    try
                    {
                        // using try/finally rather than using to avoid CA2202 warning (https://msdn.microsoft.com/en-us/library/ms182334.aspx)
                        inStream = new FileStream(fileLocation, FileMode.Open, FileAccess.Read, FileShare.None);
                        using (StreamReader streamReader = new StreamReader(inStream))
                        {
                            inStream = null; // ditto; avoid CA2202

                            UpdateNotesTextBoxOnMainThread(streamReader.ReadToEnd());
                        }
                    }
                    finally
                    {
                        if (inStream != null)
                            inStream.Dispose();
                    }

                    if (persistPath)
                        persistSettings(fileLocation);

                    return true;
                }
                catch (Exception x)
                {
                    UpdateNotesTextBoxOnMainThread("Failed to open " + fileLocation + " : " + x.Message);
                }
            }

            return false;
        }

        private void UpdateNotesTextBoxOnMainThread(string text)
        {
            if (this.NotesTextBox.Dispatcher.CheckAccess())
            {
                // This thread has access so it can update the UI thread.
                UpdateNotesTextBox(text);
            }
            else
            {
                // This thread does not have access to the UI thread.
                // Place the update method on the Dispatcher of the UI thread.
                this.NotesTextBox.Dispatcher.Invoke(DispatcherPriority.Normal, new Action<string>(UpdateNotesTextBox), text);
            }
        }

        private void UpdateNotesTextBox(string text)
        {
            this.NotesTextBox.TextChanged -= Notes_TextChanged;
            this.NotesTextBox.Text = text;
            this.NotesTextBox.TextChanged += Notes_TextChanged;
        }

        private void persistSettings(string fileLocation)
        {
            IsolatedStorageFile f = IsolatedStorageFile.GetUserStoreForAssembly();
            IsolatedStorageFileStream stream = null;
            try
            {
                // using try/finally rather than using to avoid CA2202 warning (https://msdn.microsoft.com/en-us/library/ms182334.aspx)
                stream = new IsolatedStorageFileStream(DATA_SOURCE, FileMode.Create, f);
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    stream = null; // ditto; avoids CA2202

                    writer.WriteLine(fileLocation);
                    writer.WriteLine(this.isAutoSave);
                }
            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        private void SaveFile()
        {
            // need to test if return key has been pressed; if yes, add new line
            string fileLocation = this.FilePathTextBox.Text;
            if (!string.IsNullOrEmpty(fileLocation))
            {
                try
                {
                    File.WriteAllText(fileLocation, String.Empty);
                    FileStream outStream = null;
                    try
                    {
                        // using try/finally rather than using to avoid CA2202 warning (https://msdn.microsoft.com/en-us/library/ms182334.aspx)
                        outStream = new FileStream(fileLocation, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                        using (StreamWriter streamWriter = new StreamWriter(outStream))
                        {
                            outStream = null; // ditto; avoids CA2022

                            streamWriter.Write(this.NotesTextBox.Text);
                        }
                    }
                    finally
                    {
                        if (outStream != null)
                            outStream.Dispose();
                    }
                }
                catch (Exception x)
                {
                    System.Windows.MessageBox.Show("Failed to save text: ", x.Message);
                }
            }
        }

        private void SelectFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            try
            {
                openFileDialog.FileName = this.FilePathTextBox.Text;
                openFileDialog.CheckFileExists = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string fileLocation = openFileDialog.FileName;

                if (ReadFile(fileLocation, true))
                {
                    CreateFileSystemWatcher(fileLocation);

                    this.FilePathTextBox.Text = fileLocation;
                    this.NotesTextBox.IsReadOnly = false;
                    this.isReloadRequired = false;
                }
                else
                {
                    this.FilePathTextBox.Text = string.Empty;
                    this.NotesTextBox.IsReadOnly = true;
                }
            }
        }

        private void SaveFileAs()
        {
            SaveFileDialog dialog = new SaveFileDialog();

            string fileLocation = this.FilePathTextBox.Text;

            if (string.IsNullOrEmpty(fileLocation))
            {
                // for frakked up reasons that make sense only to Microsoft, this is how you set the IntialDirectory to My Computer
                dialog.InitialDirectory = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";  // setting to My Computer to force the user to make a choice
            }
            else
            {
                FileInfo fileInfo = new FileInfo(fileLocation);
                dialog.InitialDirectory = fileInfo.DirectoryName;
                dialog.FileName = fileInfo.Name;
            }
            dialog.CheckFileExists = false;
            dialog.OverwritePrompt = true;
            DialogResult result = dialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                string chosenFileLocation = dialog.FileName;

                this.FilePathTextBox.Text = chosenFileLocation;

                SaveFile(); // save the new file

                if (ReadFile(chosenFileLocation, true)) // read the new file, which will create a new lock file (and check if we have any problems)
                {
                    this.FilePathTextBox.Text = chosenFileLocation;
                    this.NotesTextBox.IsReadOnly = false;
                }
                else
                {
                    this.FilePathTextBox.Text = string.Empty;
                    this.NotesTextBox.IsReadOnly = true;
                }
            }
        }

        private void CreateFileSystemWatcher(string fileLocation)
        {
            this.watcher = new FileSystemWatcher();
            watcher.Path = Path.GetDirectoryName(fileLocation);
            watcher.Filter = Path.GetFileName(fileLocation); 
            this.watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite;

            // Add event handlers.
            this.watcher.Changed += new FileSystemEventHandler(OnChanged);
            this.watcher.Created += new FileSystemEventHandler(OnChanged);
            this.watcher.Deleted += new FileSystemEventHandler(OnChanged);

            // Begin watching.
            this.watcher.EnableRaisingEvents = true;
        }

        #region Event Handlers

        void OnChanged(object source, FileSystemEventArgs e)
        {
            // How to handle this?  Problem appears to be that FileSystemWatcher cannot react fast enough to cope with typing...?

            //if (!this.isUpdatingSelf && !this.isReloadRequired)
            //{
            //    this.isReloadRequired = true;
            //    SetNotesTextBoxReadOnlyOnMainThread();
            //    string text = "Editing has been disabled.  Please reopen the notes file to re-enable editing.";
            //    string instruction = "The notes file has been updated by another application.";
            //    string caption = "Notes";
            //    TaskDialogResult result = TaskDialog.Show(text, instruction, caption, TaskDialogButtons.Cancel, TaskDialogIcon.Warning);
            //}
        }

        private void SetNotesTextBoxReadOnlyOnMainThread()
        {
            if (this.NotesTextBox.Dispatcher.CheckAccess())
            {
                // This thread has access so it can update the UI thread.
                SetNotesTextBoxReadOnly();
            }
            else
            {
                // This thread does not have access to the UI thread.
                // Place the update method on the Dispatcher of the UI thread.
                this.NotesTextBox.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(SetNotesTextBoxReadOnly));
            }
        }

        private void SetNotesTextBoxReadOnly()
        {
            this.NotesTextBox.IsReadOnly = true;
        }

        void NotesWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Initialize();
        }

        void NotesWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            this.isInitialized = false;
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            SelectFile();
        }

        private void Notes_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.isUpdatingSelf = true;
            if (this.isAutoSave)
            {
                this.AutoSaveLabel.Content = "Auto-save enabled...saving...";
                SaveFile();
                this.AutoSaveLabel.Content = "Auto-save enabled...";
            }
            this.isUpdatingSelf = false;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFile();
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveFileAs();
        }

        private void AutoSave_Click(object sender, RoutedEventArgs e)
        {
            ToggleAutoSave();

            persistSettings(this.FilePathTextBox.Text);
        }

        private void ToggleAutoSave()
        {
            this.isAutoSave = !this.isAutoSave;

            if (this.isAutoSave)
            {
                string text = "Clicking \"Yes\" will save the text immediately, otherwise the text will be saved when you start typing.";
                string instruction = "Save the current text now?";
                string caption = "Notes";
                TaskDialogResult result = TaskDialog.Show(text, instruction, caption, TaskDialogButtons.Yes | TaskDialogButtons.No, TaskDialogIcon.Question);
                if (result == TaskDialogResult.Yes)
                    SaveFile();
            }

            SetAutoSaveUI();
        }

        private void SetAutoSaveUI()
        {
            AutoSaveMenuItem.IsChecked = this.isAutoSave;
            SaveMenuItem.IsEnabled = !this.isAutoSave;
            SaveAsMenuItem.IsEnabled = !this.isAutoSave;

            DialogGrid.RowDefinitions[1].Height = (this.isAutoSave) ? new GridLength(1, GridUnitType.Auto) : new GridLength(0);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(!isDisposed);
        }

        private bool isDisposed;

        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                if (this.watcher != null)
                {
                    this.watcher.Changed -= new FileSystemEventHandler(OnChanged);
                    this.watcher.Created -= new FileSystemEventHandler(OnChanged);
                    this.watcher.Deleted -= new FileSystemEventHandler(OnChanged);
                    this.watcher.EnableRaisingEvents = false;
                    this.watcher.Dispose();
                    this.watcher = null;
                }

                isDisposed = true;

                GC.SuppressFinalize(this);
            }
        }
        
        #endregion
    }
}