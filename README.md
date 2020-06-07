# DupeFiles
DupeFiles scans your file system for duplicate files.
DupeFiles is a csharp dotnet core application which runs under windows, linux and probably osx.

DupeFiles checks files for for file size, hash and finally binary.

### Short demo
```
// Show help
dupefiles.exe --help

// Add a folder to the index
dupefiles.exe add --path /folder/to/add

// Scan the index for duplicates
dupefiles.exe scan
```
