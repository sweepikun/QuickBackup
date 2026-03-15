using System;
using System.Collections.Generic;

namespace QuickBackup
{
    [Serializable]
    public class FileSnapshot
    {
        public DateTime CreatedTime { get; set; }
        public string RootPath { get; set; }
        public List<FileEntry> Files { get; set; }

        public FileSnapshot()
        {
            Files = new List<FileEntry>();
            CreatedTime = DateTime.UtcNow;
        }
    }

    [Serializable]
    public class FileEntry
    {
        public string RelativePath { get; set; }
        public string Sha256 { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
    }

    public class DiffResult
    {
        public List<FileEntry> AddedFiles { get; set; }
        public List<FileEntry> ModifiedFiles { get; set; }
        public List<FileEntry> DeletedFiles { get; set; }
        public List<FileEntry> UnchangedFiles { get; set; }

        public DiffResult()
        {
            AddedFiles = new List<FileEntry>();
            ModifiedFiles = new List<FileEntry>();
            DeletedFiles = new List<FileEntry>();
            UnchangedFiles = new List<FileEntry>();
        }
    }
}
