# DupeFiles
DupeFiles scans your file system for duplicate files.
DupeFiles is a csharp dotnet core application which runs under windows, linux and probably osx.

DupeFiles checks files for for file size, hash and finally binary.

<<<<<<< HEAD
=======
![DupeFiles GIF](https://raw.githubusercontent.com/dahead/DupeFiles/master/DupeFiles.gif)

>>>>>>> ea5a6d1d8d5be10060e5749f0d04f1d83adef227
### Short demo
```
// Show help
dupefiles.exe --help

// Add a folder to the index
dupefiles.exe add --path /folder/to/add

// Scan the index for duplicates
dupefiles.exe scan

// Remove duplicate files
dupefiles.exe clean

// Purge index of non existant files
dupefiles.exe purge

// Remove all items from the index which contain the string "-copy"
dupefiles.exe remove --pattern "-copy"
```

### Quick demo
This adds, scans and cleans directory all in one run:
```
quick --path /folder/to/scan
```

### Todos
- Integrate the "analyze" option/feature
- Remove dupes after deletion/moving... from DupeGroupList

### Version history
- 1.3 (4/21/2021)
    - New option: analyze
    - Better dupe handling
    - Better output
    - Loading and saving of the found duplicate file information
    - Easy removing of found duplicates
- 1.2.1 (4/15/2021)
    - Two new output types: XML and Text
    - Cancel current action with ctrl-c
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
<<<<<<< HEAD
    - Remove option to remove certain items from the index
=======
    - Remove option to remove certain items from the index
>>>>>>> ea5a6d1d8d5be10060e5749f0d04f1d83adef227
