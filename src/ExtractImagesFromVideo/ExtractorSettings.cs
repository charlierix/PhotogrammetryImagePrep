using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xaml;

namespace ExtractImagesFromVideo
{
    /// <summary>
    /// This gets serialized and saved to %appdata%\PhotogrammetryImagePrep\ImagePrep.xml
    /// </summary>
    public class ExtractorSettings
    {
        public string BaseFolder { get; set; }
        public string YoutubeDL { get; set; }
        public string VLC { get; set; }

        public static ExtractorSettings GetSettingsFromDrive()
        {
            string filename = GetSettingsFilename();

            if (!File.Exists(filename))
            {
                return null;
            }

            try
            {
                return XamlServices.Load(filename) as ExtractorSettings;
            }
            catch
            {
                return null;
            }
        }
        public static void SaveSettingsToDrive(ExtractorSettings settings)
        {
            string filename = GetSettingsFilename();

            Directory.CreateDirectory(Path.GetDirectoryName(filename));

            XamlServices.Save(filename, settings);
        }

        private static string GetSettingsFilename()
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            folder = Path.Combine(folder, "PhotogrammetryImagePrep");
            return Path.Combine(folder, "ImagePrep.xml");
        }
    }
}
