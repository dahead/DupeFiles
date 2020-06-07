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

### Todo
- Add output of found duplicates to file
- Add interactive mode if no parameters are given?

### Version history
- 1.2.0 (07/06/2020)
    - New scan option "output" for storing the found duplicate file information.
    - Removed some errors when rescanning folders with new files which wheren't previously scanned.
    - Correctly print out found duplicates.
    - New type of how DupeFiles stores its duplicate files inside the memory.
    - Duplicates are now shown in red.
    - Redone displaying hashsum calculation.
    - Removed a lot of unnecessary log outputs.
    - Removed unnecessary file information from the index.
    - Renamed options (idscan -> scan, idplus -> add, ...)
    - Removed the option "info". Instead information about the scan is shown after the scan is done.
    - MaxFileInfo is now the default value of Long.MaxValue.
- 1.1.1
    - Remove option to remove certain items from the index