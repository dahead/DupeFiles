using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using CommandLine.Text;

namespace dupefiles
{

    public enum OutputType { Console, LogFile, Silent }

    public enum ExportType { JSON, XML, TXT }

    public enum CleaningMethod { Move, Delete, DeleteByFilenameLength, Recycle, ReplaceWithLink, ReplaceWithTextfile }

    public enum AnalyticType { GroupByOrigin, GroupByFileType }

    [Verb("add", HelpText = "Add files to the index.")]
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

    [Verb("clean", HelpText = "Tools for removing duplicate files.")]
    public class CleanOptions
    {
        [Option(Required = true, HelpText = "Type of the cleaning method.")]
        public CleaningMethod Method { get; set; }
        
        [Option(HelpText = "Filter for some cleaning methods.")]
        public string Filter { get; set; }

        [Option(HelpText = "Directory where to move the found duplicate files.")]
        public string MoveToDirectory { get; set; }
        
    }

    [Verb("analytics", HelpText = "Tools for analyzing duplicate files.")]
    public class AnalyticsOptions
    {
        [Option(Required = true, Default = AnalyticType.GroupByFileType, HelpText = "Type of the analytic.")]
        public AnalyticType Type { get; set; }
    }

    [Verb("quick", HelpText = "Quick access to common functions.")]
    public class QuickFunctions
    {
        [Option(Required = true, HelpText = "Path of the directory to scan.")]
        public string Path { get; set; }
    }

    [Verb("setup", HelpText = "Configures stuff.")]
    public class SetupOptions {

        [Option(Default = OutputType.Console, HelpText = "The output type. Console, LogFile or Silent.")] 
        public OutputType OutputType { get; set; }   

        [Option(Default = ExportType.JSON, HelpText = "The export type. JSON, XML oder Text.")] 
        public ExportType ExportType { get; set; }   

        [Option(Default = "log", HelpText = "Filename of the log file (when output is set to LogFile or XML).")] 
        public string LogFilename { get; set; }

        [Option(Default = "output", HelpText = "Filename to use for the result output.")]
        public string OutputFilename { get; set; }

        [Option(Default = true, HelpText = "Persistent mode. If set to false nothing will be written onto the disk.")] 
        public bool PersistentMode { get; set; }
    }

}