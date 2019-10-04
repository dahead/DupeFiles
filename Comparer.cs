using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;

namespace dupefiles
{
    public class Comparer
    {

        public FileIndex fidx { get; set; }

        public Comparer()
        {
            this.fidx = new FileIndex();
        }

        public void Init(string[] args)
        {

            this.fidx.Load();

            CommandLine.Parser.Default.ParseArguments<AddOptions, ScanOptions, PurgeOptions, IndexInfoOptions, SetupOptions>(args)
                .MapResult(
                (AddOptions opts) => RunAddAndReturnExitCode(opts),
                (PurgeOptions opts) => RunPurgeAndReturnExitCode(opts),
                (ScanOptions opts) => RunScanAndReturnExitCode(opts),
                (IndexInfoOptions opts) => RunIndexInfoAndReturnExitCode(opts),
                (SetupOptions opts) => RunSetupAndReturnExitCode(opts),
                // (CloneOptions opts) => RunCloneAndReturnExitCode(opts),
                errs => HandleParseError(errs));

        }

        public int HandleParseError(IEnumerable<CommandLine.Error> errors)
        {
            // throw new NotImplementedException();
            // Todo...            
            return 0;
        }

        public int RunAddAndReturnExitCode(AddOptions opt)
		{	
            if (String.IsNullOrEmpty(opt.Path))
            {
                Console.WriteLine("Path to add to the index not specified!");
                return 0;
            }           
            return this.fidx.AddDirectory(opt);
		}
		
        private int RunPurgeAndReturnExitCode(PurgeOptions opt)
		{
			//throw new NotImplementedException();
            this.fidx.Purge(opt);
            this.fidx.Save();
            return 0;
		}

		private int RunScanAndReturnExitCode(ScanOptions opt)
		{
			//throw new NotImplementedException();
            this.fidx.Scan(opt);
            return 0;
		}
		
        private int RunIndexInfoAndReturnExitCode(IndexInfoOptions opt)
		{
			//throw new NotImplementedException();
            this.fidx.Info(opt);
            return 0;
		}

        private int RunSetupAndReturnExitCode(SetupOptions opt)
        {
            this.fidx.DoSetup(opt);
            return 0;
        }

        public void Close()
        {
            if (!this.fidx.Setup.PersistentMode)
            {           
                // save index to file
                this.fidx.Save();

                // other stuff to do before closing...
                // ...

                // Write log to file
                if (this.fidx.Setup.OutputType == OutputType.LogFile)
                {
                    // use a streamwriter
                    using (StreamWriter swriter = new StreamWriter(this.fidx.Setup.LogFilename))
                    {
                        swriter.Write(this.fidx.LogFile.ToString());
                    }  
                    // System.IO.File.WriteAllText(this.fids.LogFile.ToString(), this.fids.Setup.LogFilename);
                }
            }
            else
            {
                Console.WriteLine("Persistent mode is on. No harddisk output.");
            }            
        }
    }
}