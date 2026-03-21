using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace QuickBackup
{
    public class BackupEngine
    {
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

        public FileSnapshot ScanFolder(string folderPath)
        {
            folderPath = Path.GetFullPath(folderPath);
            var snapshot = new FileSnapshot
            {
                RootPath = folderPath,
                CreatedTime = DateTime.UtcNow
            };

            var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                .Where(f => File.Exists(f)).ToArray();

            foreach (var file in files)
            {
                try
                {
                    var fi = new FileInfo(file);
                    var entry = new FileEntry
                    {
                        RelativePath = GetRelativePath(snapshot.RootPath, file),
                        Size = fi.Length,
                        LastModified = fi.LastWriteTimeUtc,
                        Sha256 = ComputeSha256(file)
                    };
                    snapshot.Files.Add(entry);
                }
                catch (Exception)
                {
                }
            }

            return snapshot;
        }

        public void SaveSnapshot(FileSnapshot snapshot, string filePath)
        {
            string json = _serializer.Serialize(snapshot);
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }

        public FileSnapshot LoadSnapshot(string filePath)
        {
            string json = File.ReadAllText(filePath, Encoding.UTF8);
            var dict = _serializer.Deserialize<Dictionary<string, object>>(json);

            var snapshot = new FileSnapshot
            {
                RootPath = dict.ContainsKey("RootPath") ? dict["RootPath"] as string : "",
                CreatedTime = dict.ContainsKey("CreatedTime") ? DateTime.Parse(dict["CreatedTime"].ToString()) : DateTime.UtcNow
            };

            if (dict.ContainsKey("Files"))
            {
                var filesList = dict["Files"] as System.Collections.ArrayList;
                if (filesList != null)
                {
                    foreach (Dictionary<string, object> fileDict in filesList)
                    {
                        var entry = new FileEntry
                        {
                            RelativePath = fileDict.ContainsKey("RelativePath") ? fileDict["RelativePath"] as string : "",
                            Sha256 = fileDict.ContainsKey("Sha256") ? fileDict["Sha256"] as string : "",
                            Size = fileDict.ContainsKey("Size") ? Convert.ToInt64(fileDict["Size"]) : 0,
                            LastModified = fileDict.ContainsKey("LastModified") ? DateTime.Parse(fileDict["LastModified"].ToString()) : DateTime.MinValue
                        };
                        snapshot.Files.Add(entry);
                    }
                }
            }

            return snapshot;
        }

        public DiffResult CompareSnapshots(FileSnapshot oldSnapshot, FileSnapshot newSnapshot)
        {
            var result = new DiffResult();

            var oldDict = new Dictionary<string, FileEntry>();
            foreach (var entry in oldSnapshot.Files)
            {
                oldDict[entry.RelativePath] = entry;
            }

            var newDict = new Dictionary<string, FileEntry>();
            foreach (var entry in newSnapshot.Files)
            {
                newDict[entry.RelativePath] = entry;
            }

            foreach (var kvp in newDict)
            {
                if (oldDict.ContainsKey(kvp.Key))
                {
                    if (oldDict[kvp.Key].Sha256 != kvp.Value.Sha256)
                    {
                        result.ModifiedFiles.Add(kvp.Value);
                    }
                    else
                    {
                        result.UnchangedFiles.Add(kvp.Value);
                    }
                }
                else
                {
                    result.AddedFiles.Add(kvp.Value);
                }
            }

            foreach (var kvp in oldDict)
            {
                if (!newDict.ContainsKey(kvp.Key))
                {
                    result.DeletedFiles.Add(kvp.Value);
                }
            }

            return result;
        }

        public void CopyChangedFiles(string sourceFolder, DiffResult changes, string outputFolder)
        {
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            foreach (var entry in changes.AddedFiles)
            {
                CopyFile(sourceFolder, outputFolder, entry.RelativePath);
            }

            foreach (var entry in changes.ModifiedFiles)
            {
                CopyFile(sourceFolder, outputFolder, entry.RelativePath);
            }

            if (changes.DeletedFiles.Count > 0)
            {
                string deletedListPath = Path.Combine(outputFolder, "_deleted.txt");
                var sb = new StringBuilder();
                foreach (var entry in changes.DeletedFiles)
                {
                    sb.AppendLine(entry.RelativePath);
                }
                File.WriteAllText(deletedListPath, sb.ToString(), Encoding.UTF8);
            }
        }

        private void CopyFile(string sourceFolder, string outputFolder, string relativePath)
        {
            string sourcePath = Path.Combine(sourceFolder, relativePath);
            string destPath = Path.Combine(outputFolder, relativePath);
            string destDir = Path.GetDirectoryName(destPath);

            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            File.Copy(sourcePath, destPath, true);
        }

        private string ComputeSha256(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = sha256.ComputeHash(stream);
                var sb = new StringBuilder();
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private string GetRelativePath(string rootPath, string filePath)
        {
            rootPath = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string fullPath = Path.GetFullPath(filePath);
            if (fullPath.StartsWith(rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return fullPath.Substring(rootPath.Length + 1);
            }
            if (fullPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                return "";
            }
            throw new InvalidOperationException("File path is not under root path: " + filePath);
        }
    }
}
