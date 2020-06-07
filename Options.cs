using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using CommandLine.Text;

namespace dupefiles
{

    public enum OutputType { Silent, Console, LogFile, XML }

    public enum LogAddType {NewLine, Append};

    [Verb("add", HelpText = "Add file contents to the index.")]
    public class AddOptions {
        [Option(Required = true, HelpText = "Path of the directory to add.")] 
        public string Path { get; set; }

        [Option(Default = true, HelpText = "Include sub directories.")] 
        public bool Recursive { get; set; }

        [Option(Default = true, HelpText = "Skip adding directory names which start with a dot.")] 
        public bool SkipDirectoriesStartingWithADot { get; set; }

        [Option(Default = "*.*", HelpText = "Pattern for file extension of files to include.")] 
        public string Pattern { get; set; }
    }

    [Verb("remove", HelpText = "Remove file contents from the index.")]
    public class RemoveOptions {
        [Option(Required = true, HelpText = "Pattern for file or directory names to remove.")] 
        public string Pattern { get; set; }
    }

    [Verb("purge", HelpText = "Purge the index of non existant files.")]
    public class PurgeOptions {
    }

    [Verb("scan", HelpText = "Scan the index for duplicate files.")]
    public class ScanOptions {

        [Option(Default = 1024*1024, HelpText = "Minimum file size in bytes for comparison to use.")] 
        public long MinSize { get; set; }

        [Option(Default = long.MaxValue, HelpText = "Maximum file size in bytes for comparison to use.")] 
        public long MaxSize { get; set; }        

    }

    [Verb("setup", HelpText = "Configures stuff.")]
    public class SetupOptions {

        [Option(Default = OutputType.Console, HelpText = "The output type. Console, LogFile, XML or Silent.")] 
        public OutputType OutputType { get; set; }   

        [Option(Default = "log", HelpText = "Filename of the log file (when output is set to LogFile or XML).")] 
        public string LogFilename { get; set; }

        [Option(Default = false, HelpText = "Persistent mode. Don't write anything to the disk.")] 
        public bool PersistentMode { get; set; }
    }

}