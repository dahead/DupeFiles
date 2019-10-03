using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using CommandLine.Text;
using dupefiles;

namespace dupefiles
{
    public class Program
    {

        // public static Comparer MyComparer { get; set; }

        public static void Main(string[] args)
        {

            // CommandLine.Parser.Default.ParseArguments<Options>(args)
            // .WithParsed<Options>(opts => RunOptionsAndReturnExitCode(opts))
            // .WithNotParsed<Options>((errs) => HandleParseError(errs));

            // // Init Code...
            // Console.CancelKeyPress += Console_CancelKeyPress;  // Register the function to cancel event
            // MyComparer = new Comparer(args);

            Comparer MyComparer = new Comparer();             
    
            Console.CancelKeyPress += delegate {
                // call methods to clean up
                MyComparer.Close();
            };

            while (true) {
                MyComparer.Init(args);
            }



        }

  
        // public static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        // {
        //     Console.WriteLine("Exiting...");
        //     MyComparer.Close();          
        //     // Termitate what I have to terminate
        //     Environment.Exit(-1);
        // }

    }
}
