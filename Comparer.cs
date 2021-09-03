using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using System.Reflection;

namespace dupefiles
{
    public class Comparer
    {

        public FileIndex fidx { get; set; }

        public Comparer()
        {
            // Create new fileindex
            this.fidx = new FileIndex();

            // ConsoleKeyInfo cki;
            // Console.Clear();

            // Establish an event handler to process key press events.
            Console.CancelKeyPress += new ConsoleCancelEventHandler(consoleCancelHandler);
        }

        public void Init(string[] args)
        {
            // load the configuration
            this.fidx.LoadSetup();

            // load the index
            this.fidx.LoadIndex();

            this.fidx.LoadDupeGroupList();

            // Parse arguments
            CommandLine.Parser.Default.ParseArguments<AddOptions, RemoveOptions, ScanOptions, PurgeOptions, QuickFunctions, CleanOptions, AnalyticsOptions, SetupOptions>(args)
                .MapResult(
                        (AddOptions opts) => RunAddAndReturnExitCode(opts),
                        (RemoveOptions opts) => RunRemoveAndReturnExitCode(opts),
                        (PurgeOptions opts) => RunPurgeAndReturnExitCode(opts),
                        (ScanOptions opts) => RunScanAndReturnExitCode(opts),
                        (QuickFunctions opts) => RunQuickFunctionsAndReturnExitCode(opts),
                        (CleanOptions opts) => RunCleanOptionsAndReturnExitCode(opts),
                        (AnalyticsOptions opts) => RunAnalyticsOptionsAndReturnExitCode(opts),
                        (SetupOptions opts) => RunSetupAndReturnExitCode(opts),
                    errs => HandleParseError(errs)
                );
        }

        protected void consoleCancelHandler(object sender, ConsoleCancelEventArgs args)
        {
            this.fidx.Cancel = true;
            this.fidx.DoOutput("Cancelling...");
            // this.fidx.SaveIndex();

            // Set the Cancel property to true to prevent the process from terminating.
            args.Cancel = true;
        }

        private int HandleParseError(IEnumerable<CommandLine.Error> errors)
        {
            return 0;
        }

        private int RunAddAndReturnExitCode(AddOptions opt)
		{
            // Task result = this.fidx.AddDirectoryAsync(opt);
            this.fidx.AddDirectory(opt);
            return 0;
		}
		
        private int RunRemoveAndReturnExitCode(RemoveOptions opt)
		{
            this.fidx.Remove(opt);
            return 0;
		}        

        private int RunPurgeAndReturnExitCode(PurgeOptions opt)
		{
            this.fidx.PurgeIndex();
            this.fidx.SaveIndex();
            return 0;
		}

		private int RunScanAndReturnExitCode(ScanOptions opt)
		{
            this.fidx.Scan(opt);
            return 0;
		}

        private int RunQuickFunctionsAndReturnExitCode(QuickFunctions opt)
        {
<<<<<<< HEAD
            SetupOptions setop = new SetupOptions() { PersistentMode = true, LogFilename = "log.txt", ExportType = ExportType.XML, OutputFilename = "output", OutputType = OutputType.Console };
            AddOptions ao = new AddOptions() { Path = opt.Path, Pattern = "*.*", Recursive = true, SkipDirectoriesStartingWithADot = true };
            ScanOptions so = new ScanOptions() { MinSize = 0, MaxSize = long.MaxValue };
=======
            ScanOptions so = new ScanOptions() { MinSize = 0, MaxSize = long.MaxValue };
            AddOptions ao = new AddOptions() { Path = opt.Path, Pattern = "*.*", Recursive = true };
            SetupOptions setop = new SetupOptions() { PersistentMode = true, LogFilename = "log.txt", ExportType = ExportType.XML, OutputFilename = "output", OutputType = OutputType.Console };
>>>>>>> ea5a6d1d8d5be10060e5749f0d04f1d83adef227
            CleanOptions copt = new CleanOptions() { Method = CleaningMethod.DeleteByFilenameLength };

            this.fidx.Setup = setop;
            this.fidx.AddDirectory(ao);
            this.fidx.Scan(so);
            this.fidx.Clean(copt);

            return 0;
        }

        private int RunCleanOptionsAndReturnExitCode(CleanOptions opt)
        {
            this.fidx.Clean(opt);
            return 0;
        }

        private int RunAnalyticsOptionsAndReturnExitCode(AnalyticsOptions opt)
        {
            this.fidx.AnalyzeResults(opt);
            return 0;
        }

        private int RunSetupAndReturnExitCode(SetupOptions opt)
        {
            this.fidx.SaveSetup(opt);
            return 0;
        }

        public void Close()
        {
            if (this.fidx.Setup.PersistentMode)
            {           
                // save index to file
                this.fidx.SaveIndex();

                // other stuff to do before closing...
                this.fidx.SaveDupeGroupList();

                // Write log to file
                this.fidx.SaveLog();
            }
        }
    }
}