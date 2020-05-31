using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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

        private Process _videoDownload = null;

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
                txtVLCLocation.Text = _settings.VLC ?? "";

                //NOTE: the text change event listeners may have fired above, but not if going from "" to ""
                RefreshFolderNames();
                txtYoutubeDLLocation_TextChanged(this, null);
                txtVideoURL_TextChanged(this, null);
                txtVLCLocation_TextChanged(this, null);
                cboVideoToExtractFrom_SelectionChanged(this, null);
                txtExtractEveryXFrames_TextChanged(this, null);
                txtTimeWindows_TextChanged(this, null);
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
        private void DownloadVideoHelp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowHelpMessage(lblDownloadVideoHelp);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
        }
        private void Hyperlink_DownloadVLC(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                ShowHelpMessage(lblVLCHelp);

                OpenURL(e.Uri.AbsoluteUri);

                e.Handled = true;
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
        }
        private void VideoToExtractFromHelp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowHelpMessage(lblVideoToExtractFromHelp);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
        }
        private void ExtractFramesHelp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowHelpMessage(lblExtractFramesHelp);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
        }
        private void TimeWindowsHelp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowHelpMessage(lblTimeWindowsHelp);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
        }

        // ------------- Session
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

        // ------------- Download Video
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

        private void txtVideoURL_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (Regex.IsMatch(txtVideoURL.Text, @"^http(s|)://\w", RegexOptions.IgnoreCase))
                {
                    txtVideoURL.Effect = null;
                }
                else
                {
                    txtVideoURL.Effect = _errorEffect;
                }
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
                if (_videoDownload != null)
                {
                    ShowErrorMessage("A download is still running");
                    return;
                }

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

                using (StreamWriter writer = File.AppendText(System.IO.Path.Combine(_sessionFolders.VideoFolder, "log.txt")))
                    writer.WriteLine(txtVideoURL.Text);

                //https://github.com/ytdl-org/youtube-dl/blob/master/README.md#options

                // Putting a number in front in case they download multiple videos.  It guarantees uniqueness and shows the order
                // that they downloaded them in
                int fileNumber = Directory.GetFiles(_sessionFolders.VideoFolder).Length;
                fileNumber++;

                string args = string.Format("-o \"{0}\\{1} - %(title)s.%(ext)s\" {2}", _sessionFolders.VideoFolder, fileNumber, txtVideoURL.Text);

                _videoDownload = new Process()
                {
                    StartInfo = new ProcessStartInfo(txtYoutubeDLLocation.Text, args),
                    EnableRaisingEvents = true,
                };
                _videoDownload.Exited += VideoDownload_Exited;
                _videoDownload.Start();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }
        }

        private void VideoDownload_Exited(object sender, EventArgs e)
        {
            try
            {
                var callback = new Action(() =>
                {
                    RefreshFolderNames();
                    _videoDownload = null;

                    if (cboVideoToExtractFrom.Items.Count == 1)
                        cboVideoToExtractFrom.SelectedIndex = 0;
                });

                if (Dispatcher.CheckAccess()) // CheckAccess returns true if you're on the dispatcher thread
                    callback();
                else
                    Dispatcher.Invoke(callback);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
        }

        // ------------- Extract Images
        private void txtVLCLocation_PreviewDragEnter(object sender, DragEventArgs e)
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
        private void txtVLCLocation_Drop(object sender, DragEventArgs e)
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

                txtVLCLocation.Text = filenames[0];
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
        }

        private void txtVLCLocation_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (File.Exists(txtVLCLocation.Text))
                {
                    txtVLCLocation.Effect = null;
                }
                else
                {
                    txtVLCLocation.Effect = _errorEffect;
                }

                _settings.VLC = txtVLCLocation.Text;
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
        }

        private void cboVideoToExtractFrom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string text = cboVideoToExtractFrom.SelectedValue?.ToString().Trim() ?? "";

                if (text == "")
                {
                    cboVideoToExtractFrom.Effect = _errorEffect;
                }
                else
                {
                    cboVideoToExtractFrom.Effect = null;
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
        }

        private void txtExtractEveryXFrames_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (Regex.IsMatch(txtExtractEveryXFrames.Text, @"^\d+$"))
                {
                    txtExtractEveryXFrames.Effect = null;
                }
                else
                {
                    txtExtractEveryXFrames.Effect = _errorEffect;
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
        }

        private void txtTimeWindows_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var windows = ParseTimeWindows(txtTimeWindows.Text);
                if (windows.errMsg == null)
                {
                    txtTimeWindows.Effect = null;
                }
                else
                {
                    txtTimeWindows.Effect = _errorEffect;
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
        }

        private void ExtractImages_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_sessionFolders.IsValid)
                {
                    ShowErrorMessage("Need to specify folder first");
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtVLCLocation.Text))
                {
                    ShowErrorMessage("Need to point to vlc.exe first");
                    return;
                }

                string video = cboVideoToExtractFrom.SelectedValue?.ToString().Trim() ?? "";
                if (video == "")
                {
                    ShowErrorMessage("Need to specify which video to extract from first");
                    return;
                }

                if (!int.TryParse(txtExtractEveryXFrames.Text, out int everyXFrames))
                {
                    ShowErrorMessage("Couldn't parse Every X Frames as an integer");
                    return;
                }
                else if (everyXFrames < 1)
                {
                    ShowErrorMessage("Every X Frames must be 1 or more");
                    return;
                }

                var timeWindows = ParseTimeWindows(txtTimeWindows.Text);
                if (!string.IsNullOrWhiteSpace(timeWindows.errMsg))
                {
                    ShowErrorMessage($"Invalid time windows: {timeWindows.errMsg}");
                    return;
                }

                EnsureFoldersExist_SaveSettings();
                if (!_sessionFolders.DoFoldersExist)
                {
                    return;
                }

                string[] subFolders = CreateImageSnapshotSubfolders(new[] { _sessionFolders.ImagesFolder, _sessionFolders.MeshroomFolder, _sessionFolders.BlenderFolder }, timeWindows.windows);

                using (StreamWriter writer = File.AppendText(System.IO.Path.Combine(_sessionFolders.ImagesFolder, "log.txt")))
                {
                    for (int cntr = 0; cntr < subFolders.Length; cntr++)
                    {
                        writer.WriteLine(string.Format("{0}\tevery {1} frame{2}\t{3}\t{4}",
                            video,
                            everyXFrames, everyXFrames == 1 ? "" : "s",
                            timeWindows.windows.Length == 0 ? "full video" : $"{timeWindows.windows[cntr].start} to {timeWindows.windows[cntr].stop} seconds",
                            timeWindows.windows.Length == 0 ? "" : timeWindows.windows[cntr].name));
                    }
                }

                //https://wiki.videolan.org/VLC_command-line_help

                var getArgs = new Func<string, string>(f => string.Format("\"{0}\" --scene-path=\"{1}\" --video-filter=scene --play-and-exit --scene-ratio={2}",
                    System.IO.Path.Combine(_sessionFolders.VideoFolder, video),
                    f,
                    everyXFrames));

                if (timeWindows.windows.Length == 0)
                {
                    Process.Start(txtVLCLocation.Text, getArgs(subFolders[0]));
                }
                else
                {
                    for (int cntr = 0; cntr < subFolders.Length; cntr++)
                    {
                        string args = getArgs(subFolders[cntr]);
                        args += string.Format(" --start-time={0} --stop-time={1}", timeWindows.windows[cntr].start, timeWindows.windows[cntr].stop);

                        Process.Start(txtVLCLocation.Text, args);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.ToString());
            }
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

            cboVideoToExtractFrom.Items.Clear();
            if (_sessionFolders.IsValid && Directory.Exists(_sessionFolders.VideoFolder))
            {
                foreach (string video in Directory.GetFiles(_sessionFolders.VideoFolder).Where(o => !o.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)).OrderBy(o => o))
                {
                    cboVideoToExtractFrom.Items.Add(System.IO.Path.GetFileName(video));
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
            // It's cleaner to just show one message at a time
            foreach (UIElement showing in _helpMessages)
                showing.Visibility = Visibility.Collapsed;

            _helpMessages.Clear();

            // Show the message
            helpLabel.Visibility = Visibility.Visible;

            _helpMessages.Add(helpLabel);

            // Reset the timer that will hide the message after a while
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

        private static (string errMsg, (int start, int stop, string name)[] windows) ParseTimeWindows(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return (null, new (int, int, string)[0]);
            }

            // Lines
            string[] lines = text.
                Replace("\r\n", "\n").
                Split('\n').
                Select(o => o.Trim()).
                Where(o => o != "").
                ToArray();

            // Regex
            var matches = lines.
                Select(o => new
                {
                    line = o,
                    match = Regex.Match(o, @"^(?<first1>\d{0,2}):(?<second1>\d{2})(?<third1>:\d{2}|)\s+(?<first2>\d{0,2}):(?<second2>\d{2})(?<third2>:\d{2}|)\s+(?<name>.+)$"),
                }).
                ToArray();

            var nonMatches = matches.
                Where(o => !o.match.Success).
                ToArray();

            if (nonMatches.Length > 0)
            {
                return (string.Format("Invalid format:\r\n{0}", string.Join("\r\n", nonMatches.Select(o => o.line))), null);
            }

            // Fields
            var fields = matches.
                Select(o => new
                {
                    first1 = o.match.Groups["first1"].Value,
                    second1 = o.match.Groups["second1"].Value,
                    third1 = o.match.Groups["third1"].Value,
                    first2 = o.match.Groups["first2"].Value,
                    second2 = o.match.Groups["second2"].Value,
                    third2 = o.match.Groups["third2"].Value,
                    name = o.match.Groups["name"].Value,
                }).
                ToArray();

            // Validate 0 - 59
            var toIntArr = new Func<string, string, string, int[]>((f, s, t) => new[]
            {
                f == "" ? 0 : int.Parse(f),     // first can be minutes or hours, so limiting hours to be between 0-59 is a bit arbitrary, but I doubt anyone is dealing with 100 hour videos
                int.Parse(s),
                t == "" ? 0 : int.Parse(t.Substring(1)),
            });

            var outOfRange = fields.Select(o => toIntArr(o.first1, o.second1, o.third1)).
                Concat(fields.Select(o => toIntArr(o.first2, o.second2, o.third2))).
                SelectMany(o => o).
                Where(o => o > 59).
                ToArray();

            if (outOfRange.Length > 0)
            {
                return (string.Format("Times must be 0 to 59 ({0})", string.Join("|", outOfRange)), null);
            }

            // Convert to int
            var toInt = new Func<string, string, string, int>((f, s, t) =>
            {
                int first = f == "" ?
                    0 :
                    int.Parse(f);

                int second = int.Parse(s);

                if (t == "")
                {
                    return (first * 60) + second;
                }
                else
                {
                    return (first * 3600) + (second * 60) + int.Parse(t.Substring(1));
                }
            });

            var asTime = fields.
                Select(o => new
                {
                    from = toInt(o.first1, o.second1, o.third1),
                    to = toInt(o.first2, o.second2, o.third2),
                    o.name,
                }).
                ToArray();

            // Validate backward (4:15 3:05)
            var backward = Enumerable.Range(0, fields.Length).
                Select(o => new
                {
                    line = lines[o],
                    asTime[o].from,
                    asTime[o].to,
                }).
                Where(o => o.from >= o.to).
                ToArray();

            if (backward.Length > 0)
            {
                return (string.Format("From time must be less than to time ({0})", string.Join("\r\n", backward.Select(o => o.line))), null);
            }

            // Validate intersecting pairs (1:00 2:00, :30 2:30)
            var intersections = new List<(int, int)>();
            for (int outer = 0; outer < asTime.Length; outer++)
            {
                for (int inner = 0; inner < asTime.Length; inner++)
                {
                    if (outer == inner)
                        continue;

                    if (asTime[outer].from >= asTime[inner].from && asTime[outer].from <= asTime[inner].to)
                        intersections.Add((outer, inner));
                    else if (asTime[outer].to >= asTime[inner].from && asTime[outer].to <= asTime[inner].to)
                        intersections.Add((outer, inner));
                }
            }

            intersections = intersections.
                Select(o => (Math.Min(o.Item1, o.Item2), Math.Max(o.Item1, o.Item2))).
                Distinct().
                ToList();

            if (intersections.Count > 0)
            {
                string[] descriptions = intersections.
                    Select(o => $"{lines[o.Item1]} - {lines[o.Item2]}").
                    ToArray();

                return (string.Format("Time windows straddle each other ({0})", string.Join("|", descriptions)), null);
            }

            return
            (
                null,
                asTime.
                    Select(o => (o.from, o.to, o.name)).
                    ToArray()
            );
        }

        private static string[] CreateImageSnapshotSubfolders(string[] parentFolders, (int start, int stop, string name)[] windows)
        {
            if(windows.Length == 0)
            {
                windows = new (int start, int stop, string name)[] { (0, 0, "full video") };
            }

            // Use a counter to make sure folders are unique (especially useful if they hit extract button multiple times)
            int startCount = parentFolders.Max(o => Directory.GetDirectories(o).Length);
            startCount++;

            string[] names = windows.
                Select((o, i) => $"{startCount + i} - {o.name}").
                ToArray();

            // This will hold the full path of each item in names, but only for the first directory in parentFolders (which needs to be the images folder)
            var retVal = new List<string>();

            for (int cntr = 0; cntr < parentFolders.Length; cntr++)
            {
                foreach (string name in names)
                {
                    string finalName = System.IO.Path.Combine(parentFolders[cntr], name);

                    if (cntr == 0)
                        retVal.Add(finalName);

                    Directory.CreateDirectory(finalName);
                }
            }

            return retVal.ToArray();
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

        /// <summary>
        /// This will replace invalid chars with underscores, there are also some reserved words that it adds underscore to
        /// </summary>
        /// <remarks>
        /// https://stackoverflow.com/questions/1976007/what-characters-are-forbidden-in-windows-and-linux-directory-names
        /// </remarks>
        /// <param name="containsFolder">Pass in true if filename represents a folder\file (passing true will allow slash)</param>
        private static string EscapeFilename_Windows(string filename, bool containsFolder = false)
        {
            StringBuilder builder = new StringBuilder(filename.Length + 12);

            int index = 0;

            // Allow colon if it's part of the drive letter
            if (containsFolder)
            {
                Match match = Regex.Match(filename, @"^\s*[A-Z]:\\", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    builder.Append(match.Value);
                    index = match.Length;
                }
            }

            // Character substitutions
            for (int cntr = index; cntr < filename.Length; cntr++)
            {
                char c = filename[cntr];

                switch (c)
                {
                    case '\u0000':
                    case '\u0001':
                    case '\u0002':
                    case '\u0003':
                    case '\u0004':
                    case '\u0005':
                    case '\u0006':
                    case '\u0007':
                    case '\u0008':
                    case '\u0009':
                    case '\u000A':
                    case '\u000B':
                    case '\u000C':
                    case '\u000D':
                    case '\u000E':
                    case '\u000F':
                    case '\u0010':
                    case '\u0011':
                    case '\u0012':
                    case '\u0013':
                    case '\u0014':
                    case '\u0015':
                    case '\u0016':
                    case '\u0017':
                    case '\u0018':
                    case '\u0019':
                    case '\u001A':
                    case '\u001B':
                    case '\u001C':
                    case '\u001D':
                    case '\u001E':
                    case '\u001F':

                    case '<':
                    case '>':
                    case ':':
                    case '"':
                    case '/':
                    case '|':
                    case '?':
                    case '*':
                        builder.Append('_');
                        break;

                    case '\\':
                        builder.Append(containsFolder ? c : '_');
                        break;

                    default:
                        builder.Append(c);
                        break;
                }
            }

            string built = builder.ToString();

            if (built == "")
            {
                return "_";
            }

            if (built.EndsWith(" ") || built.EndsWith("."))
            {
                built = built.Substring(0, built.Length - 1) + "_";
            }

            // These are reserved names, in either the folder or file name, but they are fine if following a dot
            // CON, PRN, AUX, NUL, COM0 .. COM9, LPT0 .. LPT9
            builder = new StringBuilder(built.Length + 12);
            index = 0;
            foreach (Match match in Regex.Matches(built, @"(^|\\)\s*(?<bad>CON|PRN|AUX|NUL|COM\d|LPT\d)\s*(\.|\\|$)", RegexOptions.IgnoreCase))
            {
                Group group = match.Groups["bad"];
                if (group.Index > index)
                {
                    builder.Append(built.Substring(index, match.Index - index + 1));
                }

                builder.Append(group.Value);
                builder.Append("_");        // putting an underscore after this keyword is enough to make it acceptable

                index = group.Index + group.Length;
            }

            if (index == 0)
            {
                return built;
            }

            if (index < built.Length - 1)
            {
                builder.Append(built.Substring(index));
            }

            return builder.ToString();
        }

        #endregion
    }
}
