using System;

namespace QuickBackup
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }

            string command = args[0].ToLower();

            try
            {
                switch (command)
                {
                    case "scan":
                        HandleScan(args);
                        break;
                    case "diff":
                        HandleDiff(args);
                        break;
                    case "help":
                    case "--help":
                    case "-h":
                        PrintUsage();
                        break;
                    default:
                        Console.WriteLine("Unknown command: " + command);
                        PrintUsage();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                Environment.Exit(1);
            }
        }

        static void HandleScan(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: QuickBackup scan <folder_path> <snapshot_file>");
                Console.WriteLine("  folder_path    : Folder to scan");
                Console.WriteLine("  snapshot_file  : Path to save snapshot data");
                return;
            }

            string folderPath = args[1];
            string snapshotFile = args[2];

            if (!System.IO.Directory.Exists(folderPath))
            {
                Console.WriteLine("Folder not found: " + folderPath);
                return;
            }

            Console.WriteLine("Scanning folder: " + folderPath);
            BackupEngine engine = new BackupEngine();
            var snapshot = engine.ScanFolder(folderPath);
            engine.SaveSnapshot(snapshot, snapshotFile);
            Console.WriteLine("Scan complete. " + snapshot.Files.Count + " files recorded.");
            Console.WriteLine("Snapshot saved to: " + snapshotFile);
        }

        static void HandleDiff(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("Usage: QuickBackup diff <folder_path> <snapshot_file> <output_folder>");
                Console.WriteLine("  folder_path    : Folder to scan for changes");
                Console.WriteLine("  snapshot_file  : Previous snapshot file");
                Console.WriteLine("  output_folder  : Folder to save changed files");
                return;
            }

            string folderPath = args[1];
            string snapshotFile = args[2];
            string outputFolder = args[3];

            if (!System.IO.Directory.Exists(folderPath))
            {
                Console.WriteLine("Folder not found: " + folderPath);
                return;
            }

            if (!System.IO.File.Exists(snapshotFile))
            {
                Console.WriteLine("Snapshot file not found: " + snapshotFile);
                return;
            }

            Console.WriteLine("Loading previous snapshot...");
            BackupEngine engine = new BackupEngine();
            var oldSnapshot = engine.LoadSnapshot(snapshotFile);

            Console.WriteLine("Scanning folder: " + folderPath);
            var newSnapshot = engine.ScanFolder(folderPath);

            Console.WriteLine("Comparing files...");
            var changes = engine.CompareSnapshots(oldSnapshot, newSnapshot);

            if (changes.AddedFiles.Count == 0 && changes.ModifiedFiles.Count == 0 && changes.DeletedFiles.Count == 0)
            {
                Console.WriteLine("No changes detected.");
                return;
            }

            Console.WriteLine("Changes detected:");
            Console.WriteLine("  Added:    " + changes.AddedFiles.Count + " files");
            Console.WriteLine("  Modified: " + changes.ModifiedFiles.Count + " files");
            Console.WriteLine("  Deleted:  " + changes.DeletedFiles.Count + " files");

            Console.WriteLine("Copying changed files to: " + outputFolder);
            engine.CopyChangedFiles(folderPath, changes, outputFolder);

            Console.WriteLine("Done. Changed files copied to: " + outputFolder);

            engine.SaveSnapshot(newSnapshot, snapshotFile);
            Console.WriteLine("Snapshot updated.");
        }

        static void PrintUsage()
        {
            Console.WriteLine("QuickBackup - Fast incremental backup tool");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  scan <folder_path> <snapshot_file>");
            Console.WriteLine("      Scan a folder and save file checksums to snapshot.");
            Console.WriteLine();
            Console.WriteLine("  diff <folder_path> <snapshot_file> <output_folder>");
            Console.WriteLine("      Compare folder with snapshot, copy changed files to output.");
            Console.WriteLine();
            Console.WriteLine("  help");
            Console.WriteLine("      Show this help message.");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  QuickBackup scan C:\\MyData snapshot.json");
            Console.WriteLine("  QuickBackup diff C:\\MyData snapshot.json C:\\BackupOutput");
        }
    }
}
