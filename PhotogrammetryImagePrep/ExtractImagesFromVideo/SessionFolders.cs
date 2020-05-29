using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ExtractImagesFromVideo
{
    /// <summary>
    /// This centralizes knowledge of folder names
    /// </summary>
    public class SessionFolders
    {
        private const string FOLDER_VIDEO = "Video";

        //TODO: Escape the characters
        public void Reset(string baseFolder, string session)
        {
            BaseFolder = baseFolder ?? "";
            SessionName = session ?? "";

            SessionFolder = "";
            VideoFolder = "";

            IsValid_Base = true;
            IsValid_Session = true;
            DoFoldersExist = false;     // they might exist, but until EnsureFoldersExist is called, it's unknown
            FolderExistenceErrorMessage = "EnsureFoldersExist() wasn't called";

            if (string.IsNullOrWhiteSpace(BaseFolder))
            {
                IsValid_Base = false;
                return;
            }
            else if (string.IsNullOrWhiteSpace(SessionName))
            {
                IsValid_Session = false;
                return;
            }

            SessionFolder = System.IO.Path.Combine(BaseFolder, SessionName);
            VideoFolder = System.IO.Path.Combine(SessionFolder, FOLDER_VIDEO);
        }

        public void EnsureFoldersExist()
        {
            DoFoldersExist = false;
            FolderExistenceErrorMessage = "";

            if (!IsValid_Base)
            {
                FolderExistenceErrorMessage = "Invalid base folder";
                return;
            }
            else if (!IsValid_Session)
            {
                FolderExistenceErrorMessage = "Invalid session name";
                return;
            }

            string step = "";
            try
            {
                step = "base";
                Directory.CreateDirectory(BaseFolder);

                step = "session";
                Directory.CreateDirectory(SessionFolder);

                step = "video";
                Directory.CreateDirectory(VideoFolder);

                DoFoldersExist = true;
            }
            catch (Exception ex)
            {
                FolderExistenceErrorMessage = $"Couldn't create {step} folder: {ex.Message}";
                DoFoldersExist = false;
            }
        }

        public bool IsValid => IsValid_Base && IsValid_Session;

        public bool IsValid_Base { get; private set; }
        public bool IsValid_Session { get; private set; }

        public bool DoFoldersExist { get; private set; }
        public string FolderExistenceErrorMessage { get; private set; }

        public string BaseFolder { get; private set; }
        public string SessionFolder { get; private set; }
        public string VideoFolder { get; private set; }

        public string SessionName { get; private set; }
    }
}
