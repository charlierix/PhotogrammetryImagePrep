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

                if (!int.TryParse(txtExtractEveryXFrames.Text, out int everyXFrames))        // --scene-ratio=<integer [1 .. 2147483647]>
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

                string[] subFolders = CreateImageSnapshotSubfolders(_sessionFolders.ImagesFolder, Math.Max(1, timeWindows.windows.Length), video);

                using (StreamWriter writer = File.AppendText(System.IO.Path.Combine(_sessionFolders.ImagesFolder, "log.txt")))
                {
                    for (int cntr = 0; cntr < subFolders.Length; cntr++)
                    {
                        writer.WriteLine(string.Format("{0}\tevery {1} frames\t{2}", video, everyXFrames, timeWindows.windows.Length == 0 ? "full video" : $"{timeWindows.windows[cntr].start} to {timeWindows.windows[cntr].stop} seconds"));
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

        private static (string errMsg, (int start, int stop)[] windows) ParseTimeWindows(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return (null, new (int, int)[0]);
            }

            if (!Regex.IsMatch(text, @"^[\d:,\s]+$"))
            {
                return ("Invalid characters", null);
            }

            // Regex Parse
            var matches = new List<Match>();
            foreach (Match match in Regex.Matches(text, @"(?<first>\d{0,2}):(?<second>\d{2})(?<third>:\d{2}|)"))        // ignoring the commas.  they were mentioned to help the user keep things straight
            {
                matches.Add(match);
            }

            if (matches.Count == 0)
            {
                return ("Invalid format", null);
            }

            var groups = matches.
                Select(o => new
                {
                    first = o.Groups["first"].Value,
                    second = o.Groups["second"].Value,
                    third = o.Groups["third"].Value,
                }).
                ToArray();

            // Gaps (parts of the string that regex didn't pick up)
            string unmatchedString = GetUnmatchedStringPortion(text, matches);

            if (Regex.IsMatch(unmatchedString, @"[\d:]"))
            {
                return ("Contains partial time fragment", null);
            }

            // Validate 0 - 59
            var outOfRange = groups.
                SelectMany(o => new[]
                {
                    o.first == "" ? 0 : int.Parse(o.first),     // first can be minutes or hours, so limiting hours to be between 0-59 is a bit arbitrary, but I doubt anyone is dealing with 100 hour videos
                    int.Parse(o.second),
                    o.third == "" ? 0 : int.Parse(o.third.Substring(1)),
                }).
                Where(o => o > 59).
                ToArray();

            if (outOfRange.Length > 0)
            {
                return (string.Format("Times must be 0 to 59 ({0})", string.Join("|", outOfRange)), null);
            }

            // Validate even number
            if (matches.Count % 2 == 1)
            {
                return ("Mismatched time stamps", null);
            }

            // Convert to int
            int[] asTime = groups.
                Select(o =>
                {
                    int first = o.first == "" ?
                        0 :
                        int.Parse(o.first);

                    int second = int.Parse(o.second);

                    if (o.third == "")
                    {
                        return (first * 60) + second;
                    }
                    else
                    {
                        return (first * 3600) + (second * 60) + int.Parse(o.third.Substring(1));
                    }
                }).
                ToArray();

            var sets = Enumerable.Range(0, asTime.Length / 2).
                Select(o => (asTime[o * 2], asTime[o * 2 + 1])).
                ToArray();

            // Validate backward (4:15 3:05)
            var backward = Enumerable.Range(0, sets.Length).
                Select(o => new
                {
                    match1 = matches[o * 2],
                    match2 = matches[o * 2 + 1],
                    time = sets[o],
                }).
                Where(o => o.time.Item1 >= o.time.Item2).
                Select(o => $"{o.match1} {o.match2}").
                ToArray();

            if (backward.Length > 0)
            {
                return (string.Format("From time must be less than to time ({0})", string.Join(", ", backward)), null);
            }

            // Validate intersecting pairs (1:00 2:00, :30 2:30)
            var intersections = new List<(int, int)>();
            for (int outer = 0; outer < sets.Length; outer++)
            {
                for (int inner = 0; inner < sets.Length; inner++)
                {
                    if (outer == inner)
                        continue;

                    if (sets[outer].Item1 >= sets[inner].Item1 && sets[outer].Item1 <= sets[inner].Item2)
                        intersections.Add((outer, inner));
                    else if (sets[outer].Item2 >= sets[inner].Item1 && sets[outer].Item2 <= sets[inner].Item2)
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
                    Select(o => $"{matches[o.Item1 * 2]} {matches[o.Item1 * 2 + 1]} - {matches[o.Item2 * 2]} {matches[o.Item2 * 2 + 1]}").
                    ToArray();

                return (string.Format("Time windows straddle each other ({0})", string.Join("|", descriptions)), null);
            }

            return (null, sets);
        }

        private static string GetUnmatchedStringPortion(string text, List<Match> matches)
        {
            StringBuilder retVal = new StringBuilder();

            int index = 0;

            foreach (Match match in matches)
            {
                if (match.Index > index)
                {
                    retVal.Append(text.Substring(index, match.Index - index));
                }

                index = match.Index + match.Length;
            }

            if (index < text.Length)
            {
                retVal.Append(text.Substring(index));
            }

            return retVal.ToString();
        }

        private static string[] CreateImageSnapshotSubfolders(string parentFolder, int count, string videoName)
        {
            // If the video was downloaded by this tool, it will have a digit in front.  Don't include that in the sub
            // folders, because it's just annoying
            Match startsWithNum = Regex.Match(videoName, @"^(?<remove>\d+ - ).");
            if (startsWithNum.Success)
            {
                videoName = videoName.Substring(startsWithNum.Groups["remove"].Length);
            }

            // Use a counter to make sure folders are unique (especially useful if they hit extract button multiple times)
            int startCount = Directory.GetDirectories(parentFolder).Length;
            startCount++;

            string subName = System.IO.Path.GetFileNameWithoutExtension(videoName);

            string[] retVal = Enumerable.Range(0, count).
                Select(o => System.IO.Path.Combine(parentFolder, $"{startCount + o} - {subName}")).
                ToArray();

            foreach (string childFolder in retVal)
            {
                Directory.CreateDirectory(childFolder);
            }

            return retVal;
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
