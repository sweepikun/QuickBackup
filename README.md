## QuickBackup

Fast incremental backup tool for Windows using SHA256 checksums.

### Features

- Scan folders and record SHA256 checksums for all files
- Detect changed files by comparing checksums
- Copy only changed files while preserving folder structure
- Track deleted files

### Usage

```
QuickBackup scan <folder_path> <snapshot_file>
QuickBackup diff <folder_path> <snapshot_file> <output_folder>
```

### Examples

```bash
# First scan - record all file checksums
QuickBackup scan C:\MyData snapshot.json

# Later - detect changes and backup only changed files
QuickBackup diff C:\MyData snapshot.json C:\BackupOutput
```

### Build

Requires .NET Framework 4.7.2 and Visual Studio or MSBuild.

```
msbuild QuickBackup.sln /p:Configuration=Release
```

### Output

- `snapshot.json` - JSON file containing all file paths and their SHA256 checksums
- `_deleted.txt` - List of files that were deleted since last scan (in output folder)
