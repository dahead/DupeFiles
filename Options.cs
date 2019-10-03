using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using CommandLine.Text;

namespace dupefiles
{

    public enum OutputType { Console, LogFile, XML }

    [Verb("idplus", HelpText = "Add file contents to the index.")]
    public class AddOptions {

        [Option(Default = true, HelpText = "Include sub directories when adding.")] 
        public bool Recursive { get; set; }

        [Option(Default = true, HelpText = "Skip adding directory names which start with a dot.")] 
        public bool SkipDirectoriesStartingWithADot { get; set; }

        [Option(HelpText = "Path of directory to add.")] 
        public string Path { get; set; }

        [Option(Default = "*.*", HelpText = "Pattern for file extension of files to include.")] 
        public string Pattern { get; set; }

    }

    [Verb("idpurge", HelpText = "Purge the index of non existant files.")]
    public class PurgeOptions {
    }

    [Verb("idscan", HelpText = "Scan the index for duplicate files.")]
    public class ScanOptions {

        [Option(Default = 10*1024, HelpText = "Minimum file size in bytes for comparison to use.")] 
        public long MinSize { get; set; }

    }

    [Verb("idinfo", HelpText = "Show information of the index.")]
    public class IndexInfoOptions {
    }

    [Verb("setup", HelpText = "Configures stuff.")]
    public class SetupOptions {

        [Option(Default = OutputType.Console, HelpText = "The output type. Console, LogFile or XML.")] 
        public OutputType OutputType { get; set; }   

        [Option(HelpText = "Filename of the log file (when output is set to LogFile or XML).")] 
        public string LogFilename { get; set; }

        [Option(Default = false, HelpText = "Persistent mode. Don't write anything to disk.")] 
        public bool PersistentMode { get; set; }
    }


}
