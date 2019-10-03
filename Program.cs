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

        public static void Main(string[] args)
        {

            // CommandLine.Parser.Default.ParseArguments<Options>(args)
            // .WithParsed<Options>(opts => RunOptionsAndReturnExitCode(opts))
            // .WithNotParsed<Options>((errs) => HandleParseError(errs));

            // // Init Code...
            // Console.CancelKeyPress += Console_CancelKeyPress;  // Register the function to cancel event
            // MyComparer = new Comparer(args);

            Comparer MyComparer = new Comparer();             
            MyComparer.Init(args);
    
            MyComparer.Close();
        }


    }
}
