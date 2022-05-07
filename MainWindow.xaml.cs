using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace pdf2image_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly PopplerManager popplerManager = new("oschwartz10612", "poppler-windows");

        private readonly string myDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        public MainWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void DownloadPoppler(object sender, RoutedEventArgs e)
        {
            try
            {
                Thread thAsync = new(DownloadAsync);
                thAsync.Priority = ThreadPriority.Highest;
                thAsync.Start();
            }
            catch (InvalidOperationException) { }
        }

        private async void DownloadAsync()
        {
            Dispatcher dispatcher = Application.Current.Dispatcher; // Get application dispatcher
            dispatcher.Invoke(new Action(() =>
            {
                popplerDownloadProgressBar.Visibility = Visibility.Visible; // Show progress bar
                popplerDownloadButton.Visibility = Visibility.Hidden;
            }));

            string installationPath = Directory.CreateDirectory("poppler").FullName;

            var binPath = string.Empty;
            if (!await popplerManager.IsPopplerInstalled(installationPath))
            {
                var progress = new Action<long>(value =>
                {
                    Trace.WriteLine($"Poppler : {value}% downloaded");
                    dispatcher.Invoke(new Action(() => { popplerDownloadProgressBar.Value = value; })); // Update progress bar status while downloading
                });

                binPath = await popplerManager.DownloadFileAsync(installationPath, progress);   // Start Poppler async download
            }
            else
            {
                if (await popplerManager.IsLatestPopplerInstalled(installationPath))
                    Trace.WriteLine("Poppler is up to date");
                else
                    Trace.WriteLine("Poppler is installed, but an update is available to : " + Path.GetFileNameWithoutExtension(popplerManager.ReleaseName));
                binPath = popplerManager.BinPath;
            }

            dispatcher.Invoke(new Action(() =>
            {
                popplerField.Text = binPath;    // Resulted poppler bin path
                popplerDownloadProgressBar.Visibility = Visibility.Hidden;  // Hide progress bar
                popplerDownloadButton.Visibility = Visibility.Visible;
            }));
        }

        private void Opacity_80(object sender, MouseEventArgs e) { ((Image)sender).Opacity = 0.8; }
        private void Opacity_100(object sender, MouseEventArgs e) { ((Image)sender).Opacity = 1; }

        private void ChangeDirectory(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderDlg = new();   // In .csproj : <UseWindowsForms>true</UseWindowsForms>
            System.Windows.Forms.DialogResult folderDlgResult;
            OpenFileDialog fileDlg = new();

            switch (((Image)sender).Name)
            {
                case "inputFile":
                    fileDlg.InitialDirectory = Path.GetDirectoryName(inputField.Text);

                    if (fileDlg.ShowDialog() == true)
                        inputField.Text = fileDlg.FileName;
                    break;

                case "outputDir":
                    folderDlg.SelectedPath = popplerField.Text;
                    folderDlgResult = folderDlg.ShowDialog();

                    if (folderDlgResult.ToString() != string.Empty && Directory.Exists(folderDlg.SelectedPath))
                        outputField.Text = folderDlg.SelectedPath;
                    break;

                case "popplerDir":
                    folderDlg.SelectedPath = popplerField.Text;
                    folderDlgResult = folderDlg.ShowDialog();

                    if (folderDlgResult.ToString() != string.Empty && Directory.Exists(folderDlg.SelectedPath))
                        popplerField.Text = folderDlg.SelectedPath;
                    break;

                default:
                    break;
            }
        }

        private void MenuItem_Click_Quit(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void MenuItem_Click_OpenRepo(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Atrimilan/pdf2image",
                UseShellExecute = true
            });
        }

        private void Image_MouseDown(object sender, MouseEventArgs e)
        {
            switch (((Image)sender).Name)
            {
                case "outputFileNameInfo":
                    helpFileName.IsOpen = !helpFileName.IsOpen;
                    break;
                case "popplerInfo":
                    helpPoppler.IsOpen = !helpPoppler.IsOpen;
                    break;
                default:
                    break;
            }
        }

        private void DPI_PreviewKeyDown(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new("^[0-9]*$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void OpenOutputFolder(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(Environment.GetEnvironmentVariable("WINDIR") + @"\explorer.exe", outputField.Text);
            }
            catch (InvalidOperationException) { }
        }

        private void StartConversion(object sender, RoutedEventArgs e)
        {
            var outputDir = Directory.CreateDirectory(outputField.Text);

            Process cmd = new();
            cmd.StartInfo.FileName = "pdf2img.exe";
            cmd.StartInfo.Arguments = $"-i \"{inputField.Text}\" -o \"{outputDir}/{outputFileName.Text}.{extensionField.Text.ToLower()}\" -p \"{popplerManager.BinPath}\"";
           // cmd.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            cmd.StartInfo.CreateNoWindow = true;

            cmd.Start();
            cmd.WaitForExit();  // Attendre la fin d'execution, sinon les PNG n'existeront pas
        }

      /*  public void ConvertAllPDFtoPNG()
        {
            var pdf2img = Application.streamingAssetsPath + "/pdf2img/";    // Outils de conversions PDF en Images
            var pdfDocs = Application.streamingAssetsPath + "/Framatome/PDF/";  // Mes documents PDF

            DirectoryInfo d1 = new DirectoryInfo(pdfDocs);
            DirectoryInfo dirOutput;    // Output dir qui change à chaque PDF

            foreach (var file in d1.GetFiles("*.pdf"))
            {
                dirOutput = Directory.CreateDirectory(pdf2img + "/conversions/" + file.Name.Replace(file.Extension, ""));   // Créer un répertoire au nom du PDF

                System.Diagnostics.Process cmd = new System.Diagnostics.Process();
                cmd.StartInfo.FileName = pdf2img + "pdf2img.exe";
                cmd.StartInfo.Arguments = "-i \"" + file.FullName + "\" -o \"" + dirOutput.FullName + "/" + file.Name.Replace(file.Extension, "") + ".png\" -p \"" + pdf2img + "poppler-22.01.0/Library/bin\"";
                cmd.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                cmd.StartInfo.CreateNoWindow = true;

                cmd.Start();
                cmd.WaitForExit();  // Attendre la fin d'execution, sinon les PNG n'existeront pas

                foreach (var images in dirOutput.GetFiles("*.png"))
                {
                    GetComponent<MeshRenderer>().material.mainTexture = ConvertPNGtoTexture2D(images.FullName);
                }
            }
        }*/
    }
}
