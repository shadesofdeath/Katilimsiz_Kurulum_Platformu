using HandyControl.Themes;
using HandyControl.Tools;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HandyControl.Controls;
using System;
using MessageBox = HandyControl.Controls.MessageBox;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Interop;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using Material.Icons.WPF;
using Material.Icons;
namespace Katılımsız_Kurulum_Platformu
{
    public partial class MainWindow
    {
        public ObservableCollection<ProgramInfo> DataList { get; set; }
        public class ProgramInfo : INotifyPropertyChanged
        {
            private BitmapSource _icon;
            private bool _secim;
            private string _name;
            private string _sürüm;
            private string _boyut;

            public BitmapSource Icon
            {
                get { return _icon; }
                set { _icon = value; OnPropertyChanged(); }
            }

            public bool Secim
            {
                get { return _secim; }
                set { _secim = value; OnPropertyChanged(); }
            }

            public string Name
            {
                get { return _name; }
                set { _name = value; OnPropertyChanged(); }
            }

            public string Sürüm
            {
                get { return _sürüm; }
                set { _sürüm = value; OnPropertyChanged(); }
            }

            public string Boyut
            {
                get { return _boyut; }
                set { _boyut = value; OnPropertyChanged(); }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            string ProgramPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Programlar");
            if (!Directory.Exists(ProgramPath))
            {
                Directory.CreateDirectory(ProgramPath);
            }

            string theme = Properties.Settings.Default.Theme;
            if (!string.IsNullOrEmpty(theme))
            {
                ApplicationTheme appTheme = (ApplicationTheme)Enum.Parse(typeof(ApplicationTheme), theme);
                ((App)Application.Current).UpdateTheme(appTheme);
            }
            DataList = new ObservableCollection<ProgramInfo>();
            this.DataContext = this;
            MyListView.AllowDrop = true;
            MyListView.Drop += ListView_Drop;
            
            ContextMenu contextMenu = new ContextMenu();
            MenuItem renameItem = new MenuItem { Header = "Yeniden Adlandır" };

            
            MaterialIcon materialIcon = new MaterialIcon { Kind = MaterialIconKind.RenameOutline };
            renameItem.Icon = materialIcon;
            renameItem.Click += RenameItem_Click;
            contextMenu.Items.Add(renameItem);
            MyListView.ContextMenu = contextMenu;

            OpenFolder.Click += OpenFolder_Click;
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Programlar");
            foreach (var file in Directory.GetFiles(path, "*.exe"))
            {
                var info = new FileInfo(file);
                var icon = System.Drawing.Icon.ExtractAssociatedIcon(file);
                var bitmap = icon.ToBitmap();
                var hBitmap = bitmap.GetHbitmap();

                var wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                var versionInfo = FileVersionInfo.GetVersionInfo(file);
                var version = string.IsNullOrEmpty(versionInfo.FileVersion) ? "Bilinmiyor" : versionInfo.FileVersion;

                double fileSizeInMB = (double)info.Length / 1024 / 1024;
                string fileSize = fileSizeInMB > 1024 ? $"{fileSizeInMB / 1024:0.##} GB" : $"{fileSizeInMB:0.##} MB";
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(info.Name);
                DataList.Add(new ProgramInfo
                {
                    Icon = wpfBitmap,
                    Secim = false,
                    Name = fileNameWithoutExtension,
                    Sürüm = version,
                    Boyut = fileSize
                });
            }
        }
        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Programlar");
            Process.Start("explorer.exe", path);
        }
        private void RenameItem_Click(object sender, RoutedEventArgs e)
        {
            if (MyListView.SelectedItem is ProgramInfo selectedProgram)
            {
                string oldName = selectedProgram.Name;
                string oldPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Programlar", oldName + ".exe");

                string newName = Microsoft.VisualBasic.Interaction.InputBox("Yeni ismi girin", "Yeniden Adlandır", oldName);
                if (!string.IsNullOrEmpty(newName) && newName != oldName)
                {
                    string newPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Programlar", newName + ".exe");

                    File.Move(oldPath, newPath);

                    selectedProgram.Name = newName;
                    int index = DataList.IndexOf(selectedProgram);
                    DataList.RemoveAt(index);
                    DataList.Insert(index, selectedProgram);
                }
            }
        }
        private void ListView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files)
                {
                    if (System.IO.Path.GetExtension(file).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        string destinationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Programlar", Path.GetFileName(file));

                        if (File.Exists(destinationPath))
                        {
                            MessageBox.Show("Aynı isimde bir dosya zaten var: " + Path.GetFileName(file), "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            File.Copy(file, destinationPath, true);

                            AddProgramToList(destinationPath);
                        }
                    }
                }
            }
        }

        #region Change Theme
        private void ButtonConfig_OnClick(object sender, RoutedEventArgs e) => PopupConfig.IsOpen = true;

        private void ButtonSkins_OnClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Button button)
            {
                PopupConfig.IsOpen = false;
                if (button.Tag is ApplicationTheme tag)
                {
                    ((App)Application.Current).UpdateTheme(tag);

                    Properties.Settings.Default.Theme = tag.ToString();
                    Properties.Settings.Default.Save();
                }
            }
        }

        #endregion
        private void KahveIsmarla_Click(object sender, EventArgs e) => Process.Start("https://www.buymeacoffee.com/berkayay");
        private void GithubAdresim_OnClick(object sender, EventArgs e) => Process.Start("https://github.com/shadesofdeath");
        private void ProjeKaynak_OnClick(object sender, EventArgs e) => Process.Start("https://github.com/shadesofdeath");
        private void Hakkinda_OnClick(object sender, EventArgs e)
        {
            string message = "Program Adı: Katılımsız Kurulum Platformu\n\nSürüm: v1.0\n\nGeliştirici: Berkay AY\n\nAçıklama: Bu program, katılımsız programları tek tıkla kurmanızı sağlar. Kullanıcı dostu arayüzü ve hızlı işlem süresi ile zamandan tasarruf etmenize yardımcı olur. Programın amacı, kullanıcıların zamandan tasarruf etmesini sağlamaktır. ";
            string title = "Hakkında";
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void Iban_OnClick(object sender, EventArgs e)
        {
            string message = "VakıfBank\n\nİsim : Berkay AY\n\nIBAN : TR47 0001 5800 7309 9858 32";
            string title = "Bilgi";
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private async void Kur_Click(object sender, RoutedEventArgs e)
        {
            
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Programlar");
            if (DataList.Count == 0)
            {
                string message = "Programlar klasöründe uygulama bulunamadı.";
                string title = "Bilgi";
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                int selectedProgramCount = DataList.Count(p => p.Secim);

                if (selectedProgramCount == 0)
                {
                    string message = "Hiçbir program seçilmedi.";
                    string title = "Bilgi";
                    MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }
                AppProgress.Visibility = Visibility.Visible;
                int currentProgramIndex = 0;
                int failedCount = 0;
                foreach (var program in DataList)
                {
                    if (program.Secim)
                    {
                        string filePath = Path.Combine(path, program.Name + ".exe"); 
                        if (File.Exists(filePath))
                        {
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                FileName = filePath,
                                UseShellExecute = true,
                                Verb = "runas"
                            };
                            Process process = new Process { StartInfo = startInfo };
                            try
                            {
                                process.Start();
                                this.Dispatcher.Invoke(() =>
                                {
                                    AppName.Text = $"{program.Name} kuruluyor..";
                                });
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                                failedCount++;
                            }

                            await Task.Run(() => process.WaitForExit());
                            this.Dispatcher.Invoke(() =>
                            {
                                double progressPercentage = ((double)++currentProgramIndex / selectedProgramCount) * 100;
                                AppProgress.Value = progressPercentage;
                            });
                        }
                    }
                }

                this.Dispatcher.Invoke(() =>
                {
                    if (failedCount == selectedProgramCount)
                    {
                        AppName.Text = "Uygulamaların kurulumu başarısız oldu.";
                    }
                    else if (failedCount > 0)
                    {
                        AppName.Text = "Bazı uygulamaların kurulumu başarısız oldu.";
                    }
                    else
                    {
                        AppName.Text = "Tüm uygulamalar başarıyla kuruldu.";
                    }
                });
                AppProgress.Visibility = Visibility.Hidden;
            }
        }
        private bool _selectAll = false;

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            _selectAll = !_selectAll;
            foreach (var item in DataList)
            {
                item.Secim = _selectAll;
            }
        }
        private void LoadData()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Programlar");
            foreach (var file in Directory.GetFiles(path, "*.exe"))
            {
                var info = new FileInfo(file);
                var icon = System.Drawing.Icon.ExtractAssociatedIcon(file);
                var bitmap = icon.ToBitmap();
                var hBitmap = bitmap.GetHbitmap();

                var wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                var versionInfo = FileVersionInfo.GetVersionInfo(file);
                var version = string.IsNullOrEmpty(versionInfo.FileVersion) ? "Bilinmiyor" : versionInfo.FileVersion;

                double fileSizeInMB = (double)info.Length / 1024 / 1024;
                string fileSize = fileSizeInMB > 1024 ? $"{fileSizeInMB / 1024:0.##} GB" : $"{fileSizeInMB:0.##} MB";
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(info.Name);
                DataList.Add(new ProgramInfo
                {
                    Icon = wpfBitmap,
                    Secim = false,
                    Name = fileNameWithoutExtension,
                    Sürüm = version,
                    Boyut = fileSize
                });
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            DataList.Clear();
            LoadData();
        }
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Executable files (*.exe)|*.exe";
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string fileName in openFileDialog.FileNames)
                {
                    string destinationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Programlar", Path.GetFileName(fileName));
                    File.Copy(fileName, destinationPath, true);
                    AddProgramToList(destinationPath);
                }
            }
        }

        private void Trash_Click(object sender, RoutedEventArgs e)
        {
            int selectedProgramCount = DataList.Count(p => p.Secim);

            if (selectedProgramCount == 0)
            {
                MessageBox.Show("Hiçbir uygulama seçmediniz.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("Seçili programları silmek istediğinize emin misiniz?", "Onay", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                for (int i = DataList.Count - 1; i >= 0; i--)
                {
                    if (DataList[i].Secim)
                    {
                        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Programlar", DataList[i].Name + ".exe");
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                        DataList.RemoveAt(i);
                    }
                }
            }
        }

        private void AddProgramToList(string filePath)
        {
            var info = new FileInfo(filePath);
            var icon = System.Drawing.Icon.ExtractAssociatedIcon(filePath);
            var bitmap = icon.ToBitmap();
            var hBitmap = bitmap.GetHbitmap();

            var wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
            var version = string.IsNullOrEmpty(versionInfo.FileVersion) ? "Bilinmiyor" : versionInfo.FileVersion;

            double fileSizeInMB = (double)info.Length / 1024 / 1024;
            string fileSize = fileSizeInMB > 1024 ? $"{fileSizeInMB / 1024:0.##} GB" : $"{fileSizeInMB:0.##} MB";
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(info.Name);
            DataList.Add(new ProgramInfo
            {
                Icon = wpfBitmap,
                Secim = false,
                Name = fileNameWithoutExtension,
                Sürüm = version,
                Boyut = fileSize
            });
        }

        
    }
}
