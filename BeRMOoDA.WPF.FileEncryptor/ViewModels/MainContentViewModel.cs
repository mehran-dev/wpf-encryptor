using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using BeRMOoDA.WPF.FileEncryptor.Models;
using Bermooda;


namespace BeRMOoDA.WPF.FileEncryptor.ViewModels
{
    public class MainContentViewModel : ViewModelBase
    {
        public ObservableCollection<FileModel> FilesToEncrypt { get; set; }
        public FileModel EncryptionListSelectedItem { get; set; }
        public string EncryptionPassword { get; set; }
        public string EncryptionPasswordConfirm { get; set; }
        public int CurrnetFileEncryptionProgress { get; set; }
        public int TotalEncryptionProgress { get; set; }
        public int EncryptionElapsedTime { get; set; }
        public int EncryptionRemainingTime { get; set; }

        public ObservableCollection<FileModel> FilesToDecrypt { get; set; }
        public FileModel DecryptionListSelectedItem { get; set; }
        public string DecryptionPassword { get; set; }
        public int CurrnetFileDecryptionProgress { get; set; }
        public int TotalDecryptionProgress { get; set; }
        public int DecryptionElapsedTime { get; set; }
        public int DecryptionRemainingTime { get; set; }

        long _totalSizeToEncrypt;
        long _encryptedSizeByNow;
        FileCrypto _encryptionCryptoObject;
        Thread _encryptionThread;
        bool _encryptionFinished;
        System.Threading.Timer _encryptionAsyncTimer;

        long _totalSizeToDecrypt;
        long _decryptedSizeByNow;
        FileCrypto _decryptionCryptoObject;
        Thread _decryptionThread;
        bool _decryptionFinished;
        System.Threading.Timer _decryptionAsyncTimer;

        #region RelayCommands
        //encryption commands
        RelayCommand _addFiles2EncryptCommand;
        RelayCommand _addFolders2EncryptCommand;
        RelayCommand _encryptCommand;
        RelayCommand _removeEncryptionItemsCommand;
        RelayCommand _clearEncryptionListCommand;

        //decryption commands
        RelayCommand _addFiles2DecryptCommand;
        RelayCommand _addFolders2DecryptCommand;
        RelayCommand _decryptCommand;
        RelayCommand _removeDecryptionItemsCommand;
        RelayCommand _clearDecryptionListCommand;

        #endregion

        public MainContentViewModel()
        {
            FilesToEncrypt = new ObservableCollection<FileModel>();
            FilesToDecrypt = new ObservableCollection<FileModel>();
            _encryptionCryptoObject = new FileCrypto();
            _decryptionCryptoObject = new FileCrypto();
            _encryptionCryptoObject.EncryptionProgress += new FileCrypto.EncryptionProgressHandler(_encryptionCryptoObject_EncryptionProgress);
            _encryptionCryptoObject.EncryptionFinished += new FileCrypto.EncryptionProgressHandler(_encryptionCryptoObject_EncryptionFinished);
            _decryptionCryptoObject.DecryptionFinished += new FileCrypto.EncryptionProgressHandler(_decryptionCryptoObject_DecryptionFinished);
            _decryptionCryptoObject.DecryptionProgress += new FileCrypto.EncryptionProgressHandler(_decryptionCryptoObject_DecryptionProgress);
            _encryptionFinished = false;

        }

        void _decryptionCryptoObject_DecryptionProgress(object sender, CryptoEventArgs e)
        {
            CurrnetFileDecryptionProgress = (int)e.Percentage;
            OnPropertyChanged("CurrnetFileDecryptionProgress");
            _decryptedSizeByNow += e.BufferSize;
            TotalDecryptionProgress = (int)(_decryptedSizeByNow * 100 / _totalSizeToDecrypt);
            OnPropertyChanged("TotalDecryptionProgress");
        }

        void _decryptionCryptoObject_DecryptionFinished(object sender, CryptoEventArgs e)
        {
            CurrnetFileDecryptionProgress = 0;
            OnPropertyChanged("CurrnetFileDecryptionProgress");

            if (TotalDecryptionProgress >= 100)
            {
                TotalDecryptionProgress = 0;
                OnPropertyChanged("TotalDecryptionProgress");
                _totalSizeToDecrypt = 0;
                _decryptedSizeByNow = 0;
                _decryptionFinished = true;
                _decryptionAsyncTimer.Dispose();
                WPFMessegeBox.BackgroundBrush = App.Current.Resources["BrushListBoxFinishedItem"] as Brush;
                WPFMessegeBox.BorderBrush = Brushes.Green;
                WPFMessegeBox.Show("Decryption completed successfully.", "Operation Completed", WPFMessageBoxButtons.OK, WPFMessageBoxImage.Information);
            }
            else
                OnPropertyChanged("TotalDecryptionProgress");
        }

        void _encryptionCryptoObject_EncryptionFinished(object sender, CryptoEventArgs e)
        {
            CurrnetFileEncryptionProgress = 0;
            OnPropertyChanged("CurrnetFileEncryptionProgress");

            if (TotalEncryptionProgress >= 100)
            {
                TotalEncryptionProgress = 0;
                OnPropertyChanged("TotalEncryptionProgress");
                _totalSizeToEncrypt = 0;
                _encryptedSizeByNow = 0;
                _encryptionFinished = true;
                _encryptionAsyncTimer.Dispose();
                WPFMessegeBox.BackgroundBrush = App.Current.Resources["BrushListBoxFinishedItem"] as Brush;
                WPFMessegeBox.BorderBrush = Brushes.Green;
                WPFMessegeBox.Show("Encryption completed successfully.", "Operation Completed", WPFMessageBoxButtons.OK, WPFMessageBoxImage.Information);
            }
            else
                OnPropertyChanged("TotalEncryptionProgress");
        }

        void _encryptionCryptoObject_EncryptionProgress(object sender, CryptoEventArgs e)
        {
            CurrnetFileEncryptionProgress = (int)e.Percentage;
            OnPropertyChanged("CurrnetFileEncryptionProgress");
            _encryptedSizeByNow += e.BufferSize;
            TotalEncryptionProgress = (int)(_encryptedSizeByNow * 100 / _totalSizeToEncrypt);
            OnPropertyChanged("TotalEncryptionProgress");

        }

        void CalculateElapsedTime(object state)
        {
            string mode = (string)state;
            if (mode == "encryption")
            {
                EncryptionElapsedTime++;
                OnPropertyChanged("EncryptionElapsedTime");
                if (TotalEncryptionProgress > 0)
                    EncryptionRemainingTime = EncryptionElapsedTime * 100 / TotalEncryptionProgress - EncryptionElapsedTime;
                OnPropertyChanged("EncryptionRemainingTime");
            }
            else if (mode == "decryption")
            {
                DecryptionElapsedTime++;
                OnPropertyChanged("DecryptionElapsedTime");
                if (TotalDecryptionProgress > 0)
                    DecryptionRemainingTime = DecryptionElapsedTime * 100 / TotalDecryptionProgress - DecryptionElapsedTime;
                OnPropertyChanged("DecryptionRemainingTime");
            }
        }
        void GetFiles(string directory, ref List<string> filesList)
        {
            DirectoryInfo dir = new DirectoryInfo(directory);
            if (dir.GetDirectories().Length == 0)
            {
                foreach (var file in dir.GetFiles())
                {
                    filesList.Add(file.FullName);
                }
            }
            else
            {
                foreach (var file in dir.GetFiles())
                {
                    filesList.Add(file.FullName);
                }
                foreach (var folder in dir.GetDirectories())
                {
                    GetFiles(folder.FullName, ref filesList);
                }
            }

        }//end fn

        void Encrypt()
        {
            _encryptionCryptoObject.BufferSize = 1024;
            for (int i = 1; i <= FilesToEncrypt.Count; i++)
            {
                FilesToEncrypt[i - 1].Status = App.Current.Resources["BrushListBoxProcessingItem"] as Brush;
                for (int j = i - 2; j >= 0; j--)
                {
                    FilesToEncrypt[j].Status = App.Current.Resources["BrushListBoxFinishedItem"] as Brush;
                }
                _encryptionCryptoObject.EncryptFile(FilesToEncrypt[i - 1].Name, EncryptionPassword);
                FilesToEncrypt[i - 1].Status = App.Current.Resources["BrushListBoxFinishedItem"] as Brush;
            }
            _encryptionThread.Abort();

        }

        void Decrypt()
        {
            int i = 1;
            try
            {
                _decryptionCryptoObject.BufferSize = 1024;
                for (; i <= FilesToDecrypt.Count; i++)
                {
                    FilesToDecrypt[i - 1].Status = App.Current.Resources["BrushListBoxProcessingItem"] as Brush;
                    for (int j = i - 2; j >= 0; j--)
                    {
                        FilesToDecrypt[j].Status = App.Current.Resources["BrushListBoxFinishedItem"] as Brush;
                    }
                    _decryptionCryptoObject.DecryptFile(FilesToDecrypt[i - 1].Name, DecryptionPassword);
                    FilesToDecrypt[i - 1].Status = App.Current.Resources["BrushListBoxFinishedItem"] as Brush;
                }
                _decryptionThread.Abort();
            }
            catch (InvalidPasswordException ex)
            {
                FilesToDecrypt[i - 1].Status = App.Current.Resources["BrushListBoxFailedItem"] as Brush;
                WPFMessegeBox.BackgroundBrush = App.Current.Resources["BrushListBoxFailedItem"] as Brush;
                WPFMessegeBox.BorderBrush = Brushes.Black;
                WPFMessegeBox.Show("Password for decrypting file(s) is not correct\r\nPlease check passwords and try again", "Invalid Password", WPFMessageBoxButtons.OK, WPFMessageBoxImage.Stop);
            }

        }

        #region Encryption ICommands and Handlers

        #region ICommands

        public ICommand AddFiles2EncryptCommand
        {
            get
            {
                if (_addFiles2EncryptCommand == null)
                    _addFiles2EncryptCommand = new RelayCommand(p => this.AddFiles2EncryptCommandExecute(), p => this.AddFiles2EncryptCommandCanExecute());
                return _addFiles2EncryptCommand;
            }
        }



        public ICommand AddFolders2EncryptCommand
        {
            get
            {
                if (_addFolders2EncryptCommand == null)
                    _addFolders2EncryptCommand = new RelayCommand(p => this.AddFolders2EncryptCommandExecute(), p => this.AddFolders2EncryptCommandCanExecute());
                return _addFolders2EncryptCommand;
            }
        }



        public ICommand EncryptCommand
        {
            get
            {
                if (_encryptCommand == null)
                    _encryptCommand = new RelayCommand(p => this.EncryptCommandExecute(), p => this.EncryptCommandCanExecute());
                return _encryptCommand;
            }
        }


        public ICommand RemoveEncryptionItemsCommand
        {
            get
            {
                if (_removeEncryptionItemsCommand == null)
                    _removeEncryptionItemsCommand = new RelayCommand(p => this.RemoveEncryptionItemsCommandExecute(), p => this.RemoveEncryptionItemsCommandCanExecute());
                return _removeEncryptionItemsCommand;
            }
        }
        public ICommand ClearEncryptionItemsCommand
        {
            get
            {
                if (_clearEncryptionListCommand == null)
                    _clearEncryptionListCommand = new RelayCommand(p => this.ClearEncryptionItemsCommandExecute(), p => this.ClearEncryptionItemsCommandCanExecute());
                return _clearEncryptionListCommand;
            }
        }
        #endregion

        #region Handlers

        private bool AddFiles2EncryptCommandCanExecute()
        {
            return true;
        }

        private void AddFiles2EncryptCommandExecute()
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "All files|*.*";
            fd.Title = "Select file(s) to encrypt";
            fd.FileName = "";
            fd.Multiselect = true;
            if (fd.ShowDialog() == DialogResult.OK)
            {
                if (_encryptionFinished)
                {
                    FilesToEncrypt.Clear();
                    EncryptionElapsedTime = 0;
                    EncryptionRemainingTime = 0;
                    OnPropertyChanged("EncryptionElapsedTime");
                    OnPropertyChanged("EncryptionRemainingTime");
                }
                _encryptionFinished = false;
                foreach (string fileName in fd.FileNames)
                {
                    FileInfo currentFile = new FileInfo(fileName);
                    FilesToEncrypt.Add(new FileModel() { Name = currentFile.FullName, Size = (int)(currentFile.Length / 1024) });
                    _totalSizeToEncrypt += currentFile.Length;
                }
            }
        }

        private bool AddFolders2EncryptCommandCanExecute()
        {
            return true;
        }

        private void AddFolders2EncryptCommandExecute()
        {
            FolderBrowserDialog fb = new FolderBrowserDialog();
            fb.Description = "Select folder to encrypt it's containing files";
            if (fb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (_encryptionFinished)
                {
                    FilesToEncrypt.Clear();
                    EncryptionElapsedTime = 0;
                    EncryptionRemainingTime = 0;
                    OnPropertyChanged("EncryptionElapsedTime");
                    OnPropertyChanged("EncryptionRemainingTime");
                }
                _encryptionFinished = false;
                List<string> foundFiles = new List<string>();
                GetFiles(fb.SelectedPath, ref foundFiles);
                foreach (string file in foundFiles)
                {
                    FileInfo currentFile = new FileInfo(file);
                    FilesToEncrypt.Add(new FileModel() { Name = currentFile.FullName, Size = (double)(currentFile.Length / 1024) });
                    _totalSizeToEncrypt += currentFile.Length;
                }
            }
        }

        private bool EncryptCommandCanExecute()
        {
            return true;
        }

        private void EncryptCommandExecute()
        {
            if (EncryptionPassword == EncryptionPasswordConfirm)
            {
                _encryptionAsyncTimer = new System.Threading.Timer(new TimerCallback(CalculateElapsedTime), "encryption", 0, 1000);
                _encryptionThread = new Thread(new ThreadStart(Encrypt));
                _encryptionThread.SetApartmentState(ApartmentState.STA);
                _encryptionThread.Start();
            }
            else
            {
                WPFMessegeBox.BackgroundBrush = App.Current.Resources["BrushListBoxFailedItem"] as Brush;
                WPFMessegeBox.BorderBrush = Brushes.Black;
                WPFMessegeBox.Show("Passwords do not match.\r\nPlease check entered passwords and try again", "Password Confirm Error", WPFMessageBoxButtons.OK, WPFMessageBoxImage.Error);
            }

        }

        private bool RemoveEncryptionItemsCommandCanExecute()
        {
            return true;
        }

        private void RemoveEncryptionItemsCommandExecute()
        {
            FilesToEncrypt.Remove(EncryptionListSelectedItem);
        }


        private bool ClearEncryptionItemsCommandCanExecute()
        {
            return true;
        }

        private void ClearEncryptionItemsCommandExecute()
        {
            FilesToEncrypt.Clear();
        }
        #endregion

        #endregion

        #region Decryption ICommands and Handlers

        #region ICommands

        public ICommand AddFiles2DecryptCommand
        {
            get
            {
                if (_addFiles2DecryptCommand == null)
                    _addFiles2DecryptCommand = new RelayCommand(p => this.AddFiles2DecryptCommandExecute(), p => this.AddFiles2DecryptCommandCanExecute());
                return _addFiles2DecryptCommand;
            }
        }

        public ICommand AddFolders2DecryptCommand
        {
            get
            {
                if (_addFolders2DecryptCommand == null)
                    _addFolders2DecryptCommand = new RelayCommand(p => this.AddFolders2DecryptCommandExecute(), p => this.AddFolders2DecryptCommandCanExecute());
                return _addFolders2DecryptCommand;
            }
        }

        public ICommand DecryptCommand
        {
            get
            {
                if (_decryptCommand == null)
                    _decryptCommand = new RelayCommand(p => this.DecryptCommandExecute(), p => this.DecryptCommandCanExecute());
                return _decryptCommand;
            }
        }

        public ICommand RemoveDecryptionItemsCommand
        {
            get
            {
                if (_removeDecryptionItemsCommand == null)
                    _removeDecryptionItemsCommand = new RelayCommand(p => this.RemoveDecryptionItemsCommandExecute(), p => this.RemoveDecryptionItemsCommandCanExecute());
                return _removeDecryptionItemsCommand;
            }
        }

        public ICommand ClearDecryptionItemsCommand
        {
            get
            {
                if (_clearDecryptionListCommand == null)
                    _clearDecryptionListCommand = new RelayCommand(p => this.ClearDecryptionItemsCommandExecute(), p => this.ClearDecryptionItemsCommandCanExecute());
                return _clearDecryptionListCommand;
            }
        }
        #endregion

        #region Handlers

        private bool AddFiles2DecryptCommandCanExecute()
        {
            return true;
        }

        private void AddFiles2DecryptCommandExecute()
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "All files|*.*";
            fd.Title = "Select file(s) to decrypt";
            fd.FileName = "";
            fd.Multiselect = true;
            if (fd.ShowDialog() == DialogResult.OK)
            {
                if (_decryptionFinished)
                {
                    FilesToDecrypt.Clear();
                    DecryptionElapsedTime = 0;
                    DecryptionRemainingTime = 0;
                    OnPropertyChanged("DecryptionElapsedTime");
                    OnPropertyChanged("DecryptionRemainingTime");
                }
                _decryptionFinished = false;
                foreach (string fileName in fd.FileNames)
                {
                    FileInfo currentFile = new FileInfo(fileName);
                    FilesToDecrypt.Add(new FileModel() { Name = currentFile.FullName, Size = (int)(currentFile.Length / 1024) });
                    _totalSizeToDecrypt += currentFile.Length;
                }
            }
        }

        private bool AddFolders2DecryptCommandCanExecute()
        {
            return true;
        }

        private void AddFolders2DecryptCommandExecute()
        {
            FolderBrowserDialog fb = new FolderBrowserDialog();
            fb.Description = "Select folder to decrypt it's containing files";
            if (fb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (_decryptionFinished)
                {
                    FilesToDecrypt.Clear();
                    DecryptionElapsedTime = 0;
                    DecryptionRemainingTime = 0;
                    OnPropertyChanged("DecryptionElapsedTime");
                    OnPropertyChanged("DecryptionRemainingTime");
                }
                _decryptionFinished = false;
                List<string> foundFiles = new List<string>();
                GetFiles(fb.SelectedPath, ref foundFiles);
                foreach (string file in foundFiles)
                {
                    FileInfo currentFile = new FileInfo(file);
                    FilesToDecrypt.Add(new FileModel() { Name = currentFile.FullName, Size = (double)(currentFile.Length / 1024) });
                    _totalSizeToDecrypt += currentFile.Length;
                }
            }
        }

        private bool DecryptCommandCanExecute()
        {
            return true;
        }

        private void DecryptCommandExecute()
        {

            _decryptionAsyncTimer = new System.Threading.Timer(new TimerCallback(CalculateElapsedTime), "decryption", 0, 1000);
            _decryptionThread = new Thread(new ThreadStart(Decrypt));
            _decryptionThread.SetApartmentState(ApartmentState.STA);
            _decryptionThread.Start();


        }

        private bool RemoveDecryptionItemsCommandCanExecute()
        {
            return true;
        }

        private void RemoveDecryptionItemsCommandExecute()
        {
            FilesToDecrypt.Remove(DecryptionListSelectedItem);
        }


        private bool ClearDecryptionItemsCommandCanExecute()
        {
            return true;
        }

        private void ClearDecryptionItemsCommandExecute()
        {
            FilesToDecrypt.Clear();
        }
        #endregion

        #endregion

    }//end class

}//end namespace
