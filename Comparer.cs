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
            this.fidx = new FileIndex();
        }

        public void Init(string[] args)
        {

            this.fidx.LoadSetup();

            // load the index
            this.fidx.Load();

            // show Version
            // this.fidx.DoOutput(Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);

            // Parse arguments
            CommandLine.Parser.Default.ParseArguments<AddOptions, RemoveOptions, ScanOptions, PurgeOptions, SetupOptions>(args)
                .MapResult(
                        (AddOptions opts) => RunAddAndReturnExitCode(opts),
                        (RemoveOptions opts) => RunRemoveAndReturnExitCode(opts),
                        (PurgeOptions opts) => RunPurgeAndReturnExitCode(opts),
                        (ScanOptions opts) => RunScanAndReturnExitCode(opts),
                        (SetupOptions opts) => RunSetupAndReturnExitCode(opts),
                    errs => HandleParseError(errs)
                );
        }

        private int HandleParseError(IEnumerable<CommandLine.Error> errors)
        {
            // throw new NotImplementedException();
            // Todo...            
            return 0;
        }

        private int RunAddAndReturnExitCode(AddOptions opt)
		{	

            Stopwatch sw = new Stopwatch();
            sw.Start();

            int result = this.fidx.AddDirectory(opt);
            // Task result = this.fidx.AddDirectoryAsync(opt);

            sw.Stop();
            TimeSpan ts = sw.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            this.fidx.DoOutput("Adding items took " + elapsedTime);

            return 0;
		}
		
        private int RunRemoveAndReturnExitCode(RemoveOptions opt)
		{	
            return this.fidx.Remove(opt);
		}        

        private int RunPurgeAndReturnExitCode(PurgeOptions opt)
		{
            this.fidx.Purge(opt);
            this.fidx.Save();
            return 0;
		}

		private int RunScanAndReturnExitCode(ScanOptions opt)
		{
            Stopwatch sw = new Stopwatch();
            sw.Start();

            this.fidx.Scan(opt);

            sw.Stop();
            TimeSpan ts = sw.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            this.fidx.DoOutput("Scan took " + elapsedTime);

            return 0;
		}
		
        private int RunSetupAndReturnExitCode(SetupOptions opt)
        {
            this.fidx.SaveSetup(opt);
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