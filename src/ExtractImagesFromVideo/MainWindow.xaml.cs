using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ExtractImagesFromVideo
{
    public partial class MainWindow : Window
    {
        #region Declaration Section

        private readonly Effect _errorEffect = new DropShadowEffect()
        {
            Color = ColorFromHex("DD2020"),
            Direction = 0,
            ShadowDepth = 0,
            BlurRadius = 8,
            Opacity = .7,
        };

        private readonly List<UIElement> _helpMessages = new List<UIElement>();
        private readonly DispatcherTimer _hideHelpTimer;

        private readonly SessionFolders _sessionFolders = new SessionFolders();

        private ExtractorSettings _settings = null;

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();

            Background = SystemColors.ControlBrush;

            _hideHelpTimer = new DispatcherTimer();
            _hideHelpTimer.Interval = TimeSpan.FromMinutes(3);
            _hideHelpTimer.Tick += HideHelpTimer_Tick;
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _settings = ExtractorSettings.GetSettingsFromDrive() ??
                    new ExtractorSettings();

                if (string.IsNullOrWhiteSpace(_settings.BaseFolder))
                {
                    _settings.BaseFolder = GetUniqueBaseFolder();
                }

                txtBaseFolder.Text = _settings.BaseFolder;

                txtYoutubeDLLocation.Text = _settings.YoutubeDL ?? "";

                //NOTE: the text change event listeners may have fired above, but not if going from "" to ""
                RefreshFolderNames();
                txtYoutubeDLLocation_TextChanged(this, null);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
        }

        private void HideHelpTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                _hideHelpTimer.Stop();

                foreach (UIElement element in _helpMessages)
                    element.Visibility = Visibility.Collapsed;

                _helpMessages.Clear();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
        }

        private void HelpBaseFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowHelpMessage(lblBaseFolderHelp);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
        }
        private void HelpSessionFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowHelpMessage(lblSessionFolderHelp);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
        }
        private void HelpCreateFolders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowHelpMessage(lblCreateFolders);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
        }
        private void Hyperlink_DownloadYoutubeDL(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                ShowHelpMessage(lblYoutubeDLHelp);

                OpenURL(e.Uri.AbsoluteUri);

                e.Handled = true;
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
        }
        private void VideoURLHelp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowHelpMessage(lblVideoURLHelp);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
        }

        private void txtBaseFolder_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }
        private void txtBaseFolder_Drop(object sender, DragEventArgs e)
        {
            try
            {
                string[] filenames = e.Data.GetData(DataFormats.FileDrop) as string[];

                if (filenames == null || filenames.Length == 0)
                {
                    ShowErrorMessage("No folders selected");
                    return;
                }
                else if (filenames.Length > 1)
                {
                    ShowErrorMessage("Only one folder allowed");
                    return;
                }

                txtBaseFolder.Text = filenames[0];
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
        }

        private void txtBaseFolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                RefreshFolderNames();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
        }
        private void cboSession_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                RefreshFolderNames(true);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
        }

        private void CreateFolders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureFoldersExist_SaveSettings();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
        }

        private void txtYoutubeDLLocation_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }
        private void txtYoutubeDLLocation_Drop(object sender, DragEventArgs e)
        {
            try
            {
                string[] filenames = e.Data.GetData(DataFormats.FileDrop) as string[];

                if (filenames == null || filenames.Length == 0)
                {
                    ShowErrorMessage("No files selected");
                    return;
                }
                else if (filenames.Length > 1)
                {
                    ShowErrorMessage("Only one file allowed");
                    return;
                }

                txtYoutubeDLLocation.Text = filenames[0];
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
        }

        private void txtYoutubeDLLocation_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (File.Exists(txtYoutubeDLLocation.Text))
                {
                    txtYoutubeDLLocation.Effect = null;
                }
                else
                {
                    txtYoutubeDLLocation.Effect = _errorEffect;
                }

                _settings.YoutubeDL = txtYoutubeDLLocation.Text;
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
        }

        private void DownloadVideo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_sessionFolders.IsValid)
                {
                    ShowErrorMessage("Need to specify folder first");
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtYoutubeDLLocation.Text))
                {
                    ShowErrorMessage("Need to point to youtube-dl.exe first");
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtVideoURL.Text))
                {
                    ShowErrorMessage("Need to specify url first");
                    return;
                }

                EnsureFoldersExist_SaveSettings();
                if (!_sessionFolders.DoFoldersExist)
                {
                    return;
                }

                //https://github.com/ytdl-org/youtube-dl/blob/master/README.md#options

                // Putting a number in front in case they download multiple videos.  It guarantees uniqueness and shows the order
                // that they downloaded them in
                int fileNumber = Directory.GetFiles(_sessionFolders.VideoFolder).Length;
                fileNumber++;

                string args = string.Format("-o \"{0}\\{1} - %(title)s.%(ext)s\" {2}", _sessionFolders.VideoFolder, fileNumber, txtVideoURL.Text);

                Process.Start(txtYoutubeDLLocation.Text, args);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }
        }

        private void ExtractImages_Click(object sender, RoutedEventArgs e)
        {

        }

        #endregion

        #region Private Methods

        private void EnsureFoldersExist_SaveSettings()
        {
            ShowErrorMessage();

            _sessionFolders.EnsureFoldersExist();

            if (_sessionFolders.DoFoldersExist)
            {
                _settings.BaseFolder = _sessionFolders.BaseFolder;
                ExtractorSettings.SaveSettingsToDrive(_settings);
            }
            else
            {
                ShowErrorMessage(_sessionFolders.FolderExistenceErrorMessage);
            }
        }

        private void RefreshFolderNames(bool isCalledFromSessionCombo = false)
        {
            _sessionFolders.Reset(txtBaseFolder.Text, cboSession.Text);

            lblVideoFolder.Text = _sessionFolders.VideoFolder;

            cboSession.Effect = null;
            txtBaseFolder.Effect = null;

            if (!_sessionFolders.IsValid_Base)
                txtBaseFolder.Effect = _errorEffect;
            else if (!_sessionFolders.IsValid_Session)
                cboSession.Effect = _errorEffect;

            if (!isCalledFromSessionCombo)      //playing with the list while they type causes deadlock
            {
                cboSession.Items.Clear();
                if (_sessionFolders.IsValid_Base && Directory.Exists(_sessionFolders.BaseFolder))
                {
                    foreach (string childFolder in Directory.GetDirectories(_sessionFolders.BaseFolder).OrderBy(o => o))
                    {
                        cboSession.Items.Add(System.IO.Path.GetFileName(childFolder));      // only want the portion after the last \
                    }
                }
            }
        }

        private static string GetUniqueBaseFolder()
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            for (int cntr = 0; cntr < 10000; cntr++)
            {
                string retVal = System.IO.Path.Combine(desktop, string.Format("Photogrammetry Inputs{0}", cntr == 0 ? "" : "_" + cntr.ToString()));

                if (!Directory.Exists(retVal))
                {
                    return retVal;
                }
            }

            return "";
        }

        private void OpenURL(string url)
        {
            // Got this here:
            //https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/

            try
            {
                Process.Start(url);
            }
            catch
            {
                try
                {
                    // hack because of this: https://github.com/dotnet/corefx/issues/10361
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        url = url.Replace("&", "^&");
                        Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        Process.Start("xdg-open", url);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        Process.Start("open", url);
                    }
                    else
                    {
                        throw;
                    }
                }
                catch
                {
                    ShowErrorMessage($"Couldn't open url:\r\n{url}");
                }
            }
        }

        private void ShowHelpMessage(UIElement helpLabel)
        {
            helpLabel.Visibility = Visibility.Visible;

            _helpMessages.Add(helpLabel);

            _hideHelpTimer.Stop();
            _hideHelpTimer.Start();
        }
        private void ShowErrorMessage(string message = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                txtErrorMessage.Text = "";
                txtErrorMessage.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtErrorMessage.Text = message;
                txtErrorMessage.Visibility = Visibility.Visible;
            }
        }

        #endregion
        #region Private Methods - from party people

        /// <summary>
        /// This is just a wrapper to the color converter (why can't they have a method off the color class with all
        /// the others?)
        /// </summary>
        private static Color ColorFromHex(string hexValue)
        {
            string final = hexValue;

            if (!final.StartsWith("#"))
            {
                final = "#" + final;
            }

            if (final.Length == 4)      // compressed format, no alpha
            {
                // #08F -> #0088FF
                final = new string(new[] { '#', final[1], final[1], final[2], final[2], final[3], final[3] });
            }
            else if (final.Length == 5)     // compressed format, has alpha
            {
                // #8A4F -> #88AA44FF
                final = new string(new[] { '#', final[1], final[1], final[2], final[2], final[3], final[3], final[4], final[4] });
            }

            return (Color)ColorConverter.ConvertFromString(final);
        }

        #endregion
    }
}
