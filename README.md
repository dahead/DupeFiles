# DupeFiles
Dupe Files scans your system for duplicate files.
Dupe Files is a C# DotNet Core Application which runs under windows and linux.

Dupe Files checks files for file size, MD5 Hashsum, SHA256 Hashsum and finally binary for file duplicates.

To see all possible options just run the application without parameters.

### Short demo
```
// Add a folder to the index
dupefiles.exe add --path /folder/to/add

// Scan the index for duplicates
dupefiles.exe scan
```
