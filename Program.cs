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
            // Create a new comparer
            Comparer MyComparer = new Comparer();

            // parse arguments and init
            MyComparer.Init(args);

            // Save and close.
            MyComparer.Close();
        }
    }
}