using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Threading;
using System.Xml.Serialization;
using System.Xml;
using System.Diagnostics;
// using ExtendedXmlSerializer;
// using ExtendedXmlSerializer.Configuration;

namespace dupefiles
{

    public enum InternalLogOutputType { Info, Error, Red, Green, Yellow, Blue }

    public class FileIndexItem
    {
        public string FullFilename { get; set; }
        public long Size { get; set; }
        public string Hash  { get; set; }
    }

    public class DupeGroupList : List<DupeGroup>
    {
        internal bool AddPossibleDupelicate(string filename, string hash, long size)
        {

            // no groups or not this group?
            if (this.Count == 0 || this.Where(t => t.Hash == hash).Count() == 0 )
            {
                DupeGroup g = new DupeGroup() { Hash = hash, Size = size };
                g.Dupes.Add(filename);
                this.Add(g);
                return true;
            }

            // check if we have this file in the appropiate dupe group
            foreach (DupeGroup group in this.Where(t => t.Hash == hash))
            {
                // if not a member, add it now
                if (!group.Dupes.Contains(filename))
                {
                    group.Dupes.Add(filename);
                    return true;
                }
            }

            return false;
        }
    }

    public class DupeGroup
    {
        public long Size { get; set; }
        public string Hash { get; set; }
        // public FileIndexItemList Dupes { get; set; }
        public List<string> Dupes { get; set; }
       
        public DupeGroup()
        {
            // this.Dupes = new FileIndexItemList();
            this.Dupes = new List<string>();
        }

        public List<FileInfo> GetAllFilesWithLongFilenames(out FileInfo fikeep)
        {
            // get filename length of first item
            int cur = this.Dupes[0].Length;
            int shortest = cur;
            string shortestfn = this.Dupes[0];

            List<FileInfo> result = new List<FileInfo>();

            // find shortest filename
            foreach (string fn in this.Dupes)
            {
                if (fn.Length < shortest)
                {
                    shortest = fn.Length;
                    shortestfn = fn;
                }
            }

            if (!string.IsNullOrEmpty(shortestfn))                
                fikeep = new FileInfo(shortestfn);
            else
                fikeep = null;

            foreach (string fn in this.Dupes.Where(t => t.Length > shortest))
                result.Add(new FileInfo(fn));

            return result;
        }

    }

    public class FileIndexItemList : List<FileIndexItem>
    {
    }

    public class FileIndexItemDictonary : Dictionary<string, FileIndexItem>
    {
    }

    public class FileIndex
    {

        public const string DupeGroupFilename = "dupes.json";
        public const string IndexFileName = "index.json";
        public const string SetupFileName = "config.json";
        public const int BinaryCompareBufferSize = 4096;
        public const int HashBufferSize = 1200000;

        public SetupOptions Setup { get; set; }        
        public FileIndexItemDictonary Items { get; set; }
        private DupeGroupList DupeGroups { get; set; }


        // public FileIndexItemList Dupes { get; set; }

        //public IEnumerable<IGrouping<string, FileIndexItem>> DupesGroupedByHash { get => GetDupesGroupedByHash(); }

        //private IEnumerable<IGrouping<string, FileIndexItem>> GetDupesGroupedByHash()
        //{
        //    // return this.Dupes.GroupBy(f => f.Hash, f => f);
        //    return this.Dupes.OrderByDescending(p => p.Size).GroupBy(f => f.Hash, f => f);
        //}

        public StringBuilder LogFile { get; set; }
        public bool Cancel { get; set; }

        public FileIndex()
        {
            this.Items = new FileIndexItemDictonary();
            // this.Dupes = new FileIndexItemList();
            this.DupeGroups = new DupeGroupList();
            this.Setup = new SetupOptions();
            this.LogFile = new StringBuilder();
        }

        public void ClearIndex()
        {
            this.Items.Clear();
        }

        public void LoadIndexFrom(string filename)
        {            
            try
            {
                // deserialize JSON directly from a file
                using (StreamReader file = File.OpenText(filename))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    this.Items = (FileIndexItemDictonary)serializer.Deserialize(file, typeof(FileIndexItemDictonary));
                    DoOutput($"Loaded the index {filename} with {this.Items.Count} items.", InternalLogOutputType.Green);
                }

                // remove dead files
                this.PurgeIndex();
            }
            catch (System.Exception ex)
            {
                DoOutput($"Could not load the index {filename}. Exception: {ex.Message}", InternalLogOutputType.Error);
                this.Items = new FileIndexItemDictonary();
            }
        }

        public void SaveIndex()
        {
            this.SaveIndexAs(IndexFileName);
        }

        internal void SaveIndexAs(string filename)
        {
            // serialize JSON directly to a file
            using (StreamWriter file = File.CreateText(filename))
            {
                JsonSerializer serializer = new JsonSerializer() { Formatting = Newtonsoft.Json.Formatting.Indented };
                serializer.Serialize(file, this.Items);
            }
            // DoOutput($"Index saved with {this.Items.Count()} items as {filename}.");
        }
 
        public void LoadIndex()
        {
            if (System.IO.File.Exists(IndexFileName))
                this.LoadIndexFrom(IndexFileName);
<<<<<<< HEAD
            // else
            //    DoOutput($"Index not found: {IndexFileName}.");
=======
            else
                DoOutput($"Index not found: {IndexFileName}.");
>>>>>>> ea5a6d1d8d5be10060e5749f0d04f1d83adef227
        }

        public void LoadSetup(string filename = "")
        {
            // no configuration filename given
            if (string.IsNullOrEmpty(filename))
                filename = GetApplicationFilename(SetupFileName);

            if (!System.IO.File.Exists(filename))
                return;

            // deserialize JSON directly from a file
            using (StreamReader file = File.OpenText(filename))
            {
                JsonSerializer serializer = new JsonSerializer();
                SetupOptions opt = (SetupOptions)serializer.Deserialize(file, typeof(SetupOptions));

                // nothing loaded? Create default setup.
                if (opt == null)
                    opt = new SetupOptions();
            
                // apply
                this.Setup = opt;
                // DoOutput($"Loaded setup from file {filename}.");
            }
        }

        public void SaveSetup(SetupOptions opt, string filename = "")
        {
            if (string.IsNullOrEmpty(filename))
                filename = GetApplicationFilename(SetupFileName);

            // remember setup
            this.Setup = opt;

            // serialize JSON directly to a file
            using (StreamWriter file = File.CreateText(filename))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, opt);
            }

            DoOutput($"Saved setup to file {filename}.");
        }

        /// <summary>
        /// returns a filename within the applications local data folder
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private string GetApplicationFilename(string filename)
        {
            // Use DoNotVerify in case LocalApplicationData doesn’t exist.
            string appData = Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData, System.Environment.SpecialFolderOption.DoNotVerify), "DupeFiles");

            // Ensure the directory and all its parents exist.
            DirectoryInfo di = Directory.CreateDirectory(appData);
            string result = System.IO.Path.Combine(di.FullName, filename);

            // create config file if it does not exist yet
            if (!System.IO.File.Exists(result))
            {
                FileStream fs = File.Create(result);
                fs.Close();
            }
            return result;
        }

        public void LoadDupeGroupList(string filename = "")
        {
            if (string.IsNullOrEmpty(filename))
                filename = GetApplicationFilename(DupeGroupFilename);
            // deserialize JSON directly from a file
            using (StreamReader file = File.OpenText(filename))
            {
                JsonSerializer serializer = new JsonSerializer();
                DupeGroupList data = (DupeGroupList)serializer.Deserialize(file, typeof(DupeGroupList));

                if (data == null)
                    data = new DupeGroupList();

                this.DupeGroups = data;
                // DoOutput($"Loaded dupes from file {filename}.");
            }
        }

        public void SaveDupeGroupList(string filename = "")
        {
            if (string.IsNullOrEmpty(filename))
                filename = GetApplicationFilename(DupeGroupFilename);

            // serialize JSON directly to a file
            using (StreamWriter file = File.CreateText(filename))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, this.DupeGroups);
            }

            DoOutput($"Saved dupes to file {filename}.");
        }

        private enum PromptType { YesNo, AllYesNo }

        private int Prompt(string question, PromptType t)
        {
            switch (t)
            {
                case PromptType.YesNo:
                    question = question + " [y/n]: ";
                    break;
                case PromptType.AllYesNo:
                    question = question + " [A/y/n]: ";
                    break;
                default:
                    break;
            }

            // ask the question
            Console.Write(question);
            ConsoleKey response = Console.ReadKey(false).Key;
            Console.WriteLine();

            // return
            if (response == ConsoleKey.A)
                return 2;
            else if (response == ConsoleKey.Y)
                return 1;
            else 
                return 0;
        }

        internal void SaveLog()
        {
            if (this.Setup.OutputType == OutputType.LogFile)
            {
                // use a streamwriter
                using (StreamWriter swriter = new StreamWriter(this.Setup.LogFilename))
                {
                    swriter.Write(this.LogFile.ToString());
                }
                // System.IO.File.WriteAllText(this.fids.LogFile.ToString(), this.fids.Setup.LogFilename);
            }
        }

        public void DoOutput(string output = "", InternalLogOutputType t = InternalLogOutputType.Info)
        {
            // Return if silent
            if (this.Setup.OutputType == OutputType.Silent)
                return;

            // 
            ConsoleColor before = Console.ForegroundColor;
            switch (this.Setup.OutputType)
            {
                case OutputType.Console:

                    switch (t)
                    {
                        case InternalLogOutputType.Info:
                            Console.WriteLine(output);
                            break;
                        case InternalLogOutputType.Red:
                        case InternalLogOutputType.Error:
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Error.WriteLine(output);
                            Console.ForegroundColor = before;
                            break;
                        case InternalLogOutputType.Green:
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Error.WriteLine(output);
                            Console.ForegroundColor = before;
                            break;
                        case InternalLogOutputType.Yellow:
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Error.WriteLine(output);
                            Console.ForegroundColor = before;
                            break;
                        case InternalLogOutputType.Blue:
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.Error.WriteLine(output);
                            Console.ForegroundColor = before;
                            break;
                        default:
                            Console.WriteLine(output);
                            break;
                    }

                    break;                
                case OutputType.LogFile:
                    this.LogFile.AppendLine(output);
                    break;
                case OutputType.Silent:
                    // output nothing
                    break;
            }
        }

        public void AddDirectory(AddOptions opt)
        {
            DirectoryInfo basedi = null;
            try
            {
                basedi = new DirectoryInfo(opt.Path);
                if (!basedi.Exists)
                {
                    DoOutput($"Error! Directory does not exist {opt.Path}!", InternalLogOutputType.Error);
                    return;
                }
            }
            catch (System.Exception ex)
            {
                DoOutput($"Exception: {ex.Message}", InternalLogOutputType.Error);
                return;
            }

            // Statistics
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Start
            DoOutput($"Adding/updating index with content of {opt.Path}. Please stand by...");

            // for each file
            int fc = 0;
            IEnumerable<FileInfo> list = EnumerateFilesRecursive(opt); //.Where(t => t.Exists == true);
            foreach (FileInfo fi in list)
            {
                try
                {
                    // Only add if not already present
                    if (!this.Items.ContainsKey(fi.FullName))
                    {
                        // Create new File Index Item
                        FileIndexItem newitem = new FileIndexItem()
                        {
                            FullFilename = fi.FullName,
                            Size = fi.Length,
                        };
                        // Add new item to index
                        this.Items.Add(newitem.FullFilename, newitem);
                        fc += 1;
                    }               
                }
<<<<<<< HEAD
                // catch (System.ArgumentException argex)
                // {
                //     // DoOutput($"Exception: {argex.Message}", InternalLogOutputType.Error);
                // }
=======
                catch (System.ArgumentException argex)
                {
                    // DoOutput($"Exception: {argex.Message}", InternalLogOutputType.Error);
                }
>>>>>>> ea5a6d1d8d5be10060e5749f0d04f1d83adef227
                catch (System.Exception ex)
                {
                    DoOutput($"Exception: {ex.Message}", InternalLogOutputType.Error);
                }

                // log every 100th new file we add to the output so we see something
                if (fc % 100 == 0 && fc != 0)
                    // DoOutput($"- {fi.FullName}");
                    Console.Write(".");

                // Cancel?
                if (Cancel)
                    return;

            }
            // Done
            DoOutput(Environment.NewLine + $"Added directory {opt.Path} with {fc} items.", InternalLogOutputType.Green);

            // Statistics
            sw.Stop();
            TimeSpan ts = sw.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            this.DoOutput("Adding items took " + elapsedTime);
        }

        public void Remove(RemoveOptions opt)
        {
            int fc = 0;
            // foreach (KeyValuePair<string, FileIndexItem> item in this.Items)
            foreach (KeyValuePair<string, FileIndexItem> item in this.Items.Where(t => t.Key.Contains(opt.Pattern)))
            {
                // if (item.Key.Contains(opt.Pattern))
                // {
                    this.Items.Remove(item.Key);
                    fc += 1;
                // }
            }
            DoOutput($"Removed {fc} items from the index.");
        }

        private IEnumerable<FileInfo> EnumerateFilesRecursive(AddOptions opt)
        {
            var todo = new Queue<string>();
            todo.Enqueue(opt.Path);

            // while we have items to process
            // and the use wont cancel
            while (todo.Count > 0 && !this.Cancel)
            {
                string dir = todo.Dequeue();
                string[] subdirs = new string[0];
                string[] files = new string[0];

                DirectoryInfo di = new DirectoryInfo(dir);

                // Option: Skip directories starting with a dot?
                if (opt.SkipDirectoriesStartingWithADot)
                if (di.Name.StartsWith("."))
                    continue;

                try
                {
                    if (opt.Recursive)
                        subdirs = Directory.GetDirectories(dir);
                    else
                        subdirs = new string[] {dir};
                }                
                catch (IOException ex)
                {
                    this.DoOutput($"IO Exception: {ex.Message}", InternalLogOutputType.Error);                    
                    continue;
                }
                catch (System.UnauthorizedAccessException ex)
                {
                    DoOutput($"UnauthorizedAccess Exception: {ex.Message}", InternalLogOutputType.Error);
                    continue;
                }       
                foreach (string subdir in subdirs)
                {
                    // Option: Skip directories starting with a dot?
                    if (opt.SkipDirectoriesStartingWithADot)
                        if (subdir.StartsWith("."))
                            continue;
                    try
                    {
                        todo.Enqueue(subdir);
                    }
                    catch (System.Exception)                    
                    {                   
                        continue;
                    }
                }

                try
                {
                    files = Directory.GetFiles(dir, opt.Pattern);
                }                
                catch (IOException)
                {
                    // DoOutput($"IO Exception: {ex.Message}");
                    continue;
                }
                catch (System.UnauthorizedAccessException)
                {
                    // DoOutput($"UnauthorizedAccess Exception: {ex.Message}");
                    continue;
                }

                // Return all files
                foreach (string filename in files)
                {
                    yield return new FileInfo(filename);
                }
            }
        }

        private string CalculateSHA256(string filename)
        {
            if (!System.IO.File.Exists(filename))
                {return string.Empty;}
            try
            {
                using (var sha = SHA256.Create())
                {
                    using (var stream = new BufferedStream(File.OpenRead(filename), HashBufferSize))
                    {
                        // DoOutput($"Calculating sha256 hash for {filename}.");
                        var hash = sha.ComputeHash(stream);
                        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }
                }                    
            }
<<<<<<< HEAD
            catch (System.Exception)
=======
            catch (System.Exception ex)            
>>>>>>> ea5a6d1d8d5be10060e5749f0d04f1d83adef227
            {
                // DoOutput($"Exception when creating sha256 hash: {ex.Message}.");
                return "ErrorHash";
            }                   
        }

        private string CalculateMD5(string filename)
        {
            if (!System.IO.File.Exists(filename))
                return string.Empty;
            try
            {
                using (var md5 = MD5.Create())
                {
<<<<<<< HEAD
                    using (var stream = new BufferedStream(File.OpenRead(filename), HashBufferSize))
=======
                    using (var stream = new BufferedStream(File.OpenRead(filename), 1200000))
>>>>>>> ea5a6d1d8d5be10060e5749f0d04f1d83adef227
                    {
                        // DoOutput($"Calculating sha256 hash for {filename}.");
                        var hash = md5.ComputeHash(stream);
                        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
<<<<<<< HEAD
            catch (System.Exception)
=======
            catch (System.Exception ex)
>>>>>>> ea5a6d1d8d5be10060e5749f0d04f1d83adef227
            {
                // DoOutput($"Exception when creating MD5 hash: {ex.Message}.");
                return "ErrorHash";
            }
        }

        //public static bool AreFileContentsEqual(FileInfo fi1, FileInfo fi2) =>
        //    fi1.Length == fi2.Length &&
        //    (fi1.Length == 0 || File.ReadAllBytes(fi1.FullName).SequenceEqual(
        //                        File.ReadAllBytes(fi2.FullName)));

        private static bool StreamsContentsAreEqual(FileInfo file1, FileInfo file2)
        {
            const int bufferSize = BinaryCompareBufferSize;
            var buffer1 = new byte[bufferSize];
            var buffer2 = new byte[bufferSize];

            using (FileStream fs1 = file1.OpenRead())
            using (FileStream fs2 = file2.OpenRead())
            {
                while (true)
                {
                    int count1 = fs1.Read(buffer1, 0, bufferSize);
                    int count2 = fs2.Read(buffer2, 0, bufferSize);

                    if (count1 != count2)
                        return false;

                    if (count1 == 0)
                        return true;

                    int iterations = (int)Math.Ceiling((double)count1 / sizeof(Int64));
                    for (int i = 0; i < iterations; i++)
                    {
                        if (BitConverter.ToInt64(buffer1, i * sizeof(Int64)) != BitConverter.ToInt64(buffer2, i * sizeof(Int64)))
                        {
                            return false;
                        }
                    }
                }
            }
        }

        public void Scan(ScanOptions opt)
        {
            if (Cancel)
                return;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            // clear list
            this.DupeGroups.Clear();

            // filter items
            var filterdItems = this.Items.Where(d => d.Value.Size >= opt.MinSize && d.Value.Size <= opt.MaxSize).ToList();

            // is there anything to scan?
            if (filterdItems.Count() == 0)
            {
                DoOutput($"Nothing to scan, the index is empty. Please add items to the index first by using the command 'idplus'.");
                return;
            }

            // Get all file size duplicates without comparing with our self (t.key)
            DoOutput($"Starting base scan on {filterdItems.Count} filtered items...");
            IEnumerable<IGrouping<long, KeyValuePair<string, FileIndexItem>>> fsd =
                filterdItems.GroupBy(f => f.Value.Size, f => f);

            DoOutput("Done.", InternalLogOutputType.Green);

            DoOutput($"Calculating hashes for {fsd.Count()} file size duplicates...");
<<<<<<< HEAD
            int counter = 0;
=======
>>>>>>> ea5a6d1d8d5be10060e5749f0d04f1d83adef227
            foreach (IGrouping<long, KeyValuePair<string, FileIndexItem>> g in fsd)
            {
                // Only when we have more than one file
                if (g.Count() > 1)
                {
                    // DoOutput($"Calculating hash for {g.Count()} file size duplicates {BytesToString(g.Key)}.");
                    foreach (KeyValuePair<string, FileIndexItem> sub in g)
                    {
                        if (string.IsNullOrEmpty(sub.Value.Hash))
                        {
                            // var x = Task.Run(() => CalculateSHA256(sub.Value.FullFilename));
                            var x = Task.Run(() => CalculateMD5(sub.Value.FullFilename));
                            do
                            {
                                // output some progress
<<<<<<< HEAD
                            if (counter % 1000 == 0)
                                Console.Write(".");
=======
                                // Console.Write(".");
>>>>>>> ea5a6d1d8d5be10060e5749f0d04f1d83adef227
                            } while (!x.IsCompleted);
                            // remember hash
                            this.Items[sub.Value.FullFilename].Hash = x.Result;
                        }
                    }
                }

<<<<<<< HEAD
                counter += 1;

=======
>>>>>>> ea5a6d1d8d5be10060e5749f0d04f1d83adef227
                // Cancel?
                if (Cancel)
                    return;
            }
            DoOutput(Environment.NewLine + "Done.", InternalLogOutputType.Green);

            // Binary compare sha256 dupes
<<<<<<< HEAD
            counter = 1;
=======
            int counter = 1;
>>>>>>> ea5a6d1d8d5be10060e5749f0d04f1d83adef227
            int dupesfound = 0;
            // Get all files with a hash
            IEnumerable<IGrouping<string, KeyValuePair<string, FileIndexItem>>> possibledupes =
                this.Items.Where(t => t.Value.Hash != null).GroupBy(f => f.Value.Hash, f => f);

            // Binary compare sha256 dupes
            DoOutput($"Found {possibledupes.Count()} possible hash groups for binary comparism. Comparing now...");
            counter = 1;
            foreach (IGrouping<string, KeyValuePair<string, FileIndexItem>> dupegroup in possibledupes)
            {
                // Output some progress
                if (counter % 100 == 0)
                    Console.Write(".");

                // compare files against each other
                if (dupegroup.Count() > 1)
                {
                    foreach (KeyValuePair<string, FileIndexItem> mainitem in dupegroup)
                    {
                        FileInfo mainfile = new FileInfo(mainitem.Value.FullFilename);
                        var otherfiles = dupegroup.Where(t => t.Value.Hash == dupegroup.Key && t.Value.FullFilename != mainitem.Value.FullFilename);
                        foreach (var subitem in otherfiles)
                        {
                            FileInfo subfile = new FileInfo(subitem.Value.FullFilename);                          
                            bool identical = false;
                            try
                            {
                                // Compare
                                // DoOutput(" Comparing file " + subitem.Value.FullFilename);
                                identical = StreamsContentsAreEqual(mainfile, subfile);

                                // Dupe found
                                if (identical)
                                    if (this.DupeGroups.AddPossibleDupelicate(subitem.Value.FullFilename, subitem.Value.Hash, subitem.Value.Size))
                                        dupesfound += 1;
                            }
                            catch (System.Exception)
                            {
                                // DoOutput($"Error comparing files {file1.Name} and {file2.Name}. Error: {ex.Message}.");
                            }
                        }
                    }
                }
                counter += 1;

                // Cancel?
                if (Cancel)
                    return;
            }
            DoOutput(Environment.NewLine + "Done.", InternalLogOutputType.Green);

            // Option: save output to file...
            if (!string.IsNullOrEmpty(this.Setup.OutputFilename))
                this.ExportResults();

            // Finished.
            long totalsize = this.DupeGroups.Sum(item => item.Size);
            DoOutput($"Found a total of {dupesfound} duplicates files with a size of {BytesToString(totalsize)}:");

            // Statistics
            sw.Stop();
            TimeSpan ts = sw.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            DoOutput("Scan took " + elapsedTime);

            // Output results
            if (this.Setup.OutputType == OutputType.Console && this.DupeGroups.Count > 0)
                if (Prompt("Do you want to view the found duplicates now?", PromptType.YesNo) == 1)
                    this.ShowDupes();
        }

        private void ShowDupes()
        {
            foreach (DupeGroup g in this.DupeGroups)
            {
                DoOutput($" Hash: {g.Hash}  [{BytesToString(g.Size)}] ", InternalLogOutputType.Yellow);
                foreach (string item in g.Dupes)
                    DoOutput($"     {item}");
            }
        }

        /// <summary>
        /// Convert bytes to human readable string
        /// Thanks to https://stackoverflow.com/questions/281640/how-do-i-get-a-human-readable-file-size-in-bytes-abbreviation-using-net
        /// </summary>
        /// <param name="byteCount"></param>
        /// <returns></returns>
        public String BytesToString(long byteCount)
        {
            string[] suf = {"B", "KB", "MB", "GB", "TB", "PB", "EB"}; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }

        public void PurgeIndex()
        {
            // remove dead files from the index
            int counter = 0;
            foreach (KeyValuePair<string, FileIndexItem> item in this.Items)
            {
                if (!System.IO.File.Exists(item.Key))
                {
                    this.Items.Remove(item.Key);
                    counter += 1;
                }
            }
            this.DoOutput($"Purged {counter} files from the index.");
        }

        internal void AnalyzeResults(AnalyticsOptions opt)
        {
            // create html file with balken diagramms


            // todo: export to html table, graphs, ...

            throw new NotImplementedException();
        }

        internal void Clean(CleanOptions opt)
        {
            bool PromptEveryTime = true;
            int res = 0;

            // checks
            if (opt.Method == CleaningMethod.Move && string.IsNullOrEmpty(opt.MoveToDirectory))
            {
                DoOutput("Error! No destination directory specified!", InternalLogOutputType.Error);
                return;
            }

            // lets go
            foreach (var group in this.DupeGroups)
            {
                // cancel?
                if (Cancel)
                    return;

                DoOutput($"Cleaning hash group {group.Hash} with a size of {BytesToString(group.Size)} and {group.Dupes.Count} items.", InternalLogOutputType.Yellow);

                // Delete by filename length
                if (opt.Method == CleaningMethod.DeleteByFilenameLength)
                {
                    FileInfo fikeep;
                    List<FileInfo> subitems = group.GetAllFilesWithLongFilenames(out fikeep);

                    // continue if we have nothing
                    if (subitems.Count() < 2 && fikeep == null)
                        continue;

                    if (PromptEveryTime)
                    {
                        foreach (FileInfo subitem in subitems)
                        {
                            DoOutput($" Delete: {subitem.FullName}");
                        }
                        // keep file
                        DoOutput($"  Keep: {fikeep.FullName}");
                        res = Prompt($"Delete all files and keep the one ?", PromptType.AllYesNo);
                    }

                    if (res == 0)
                        continue;

                    if (res == 1 || res == 2 || !PromptEveryTime)
                    {
                        foreach (FileInfo fi in subitems)
                        {
                            try
                            {
                                fi.Delete();
                                group.Dupes.Remove(fi.FullName);
                            }
                            catch (Exception fdex)
                            {
                                DoOutput($"Error deleting file {fi.FullName}. Exception: {fdex.Message}", InternalLogOutputType.Error);
                            }
                        }
                    }

                    if (res == 2)
                        PromptEveryTime = false;

                    continue;
                }

                // else perform other actions
                foreach (var item in group.Dupes)
                {
                    // continue if user set filter and we have no match
                    if (!string.IsNullOrEmpty(opt.Filter))
                        if (!item.Contains(opt.Filter))
                            continue;                 

                    // other methods
                    FileInfo fi = new FileInfo(item);
                    switch (opt.Method)
                    {
                        case CleaningMethod.Move:
                            string newfn = System.IO.Path.Combine(opt.MoveToDirectory, fi.Name);
                            if (PromptEveryTime)
                                res = Prompt($"Move file {fi.FullName} to {newfn} ?", PromptType.AllYesNo);

                            if (res == 0)
                                continue;

                            if (res == 1 || res == 2 || !PromptEveryTime)
                            {
                                try
                                {
                                    fi.MoveTo(newfn);
                                    group.Dupes.Remove(newfn);
                                }
                                catch (Exception mfex)
                                {
                                    DoOutput($"Error moving file {fi.FullName}. Exception: {mfex.Message}", InternalLogOutputType.Error);
                                }
                            }

                            if (res == 2)
                                PromptEveryTime = false;

                            break;

                        case CleaningMethod.Delete:
                            if (PromptEveryTime)
                                res = Prompt($"Delete file {fi.FullName} ?", PromptType.AllYesNo);

                            if (res == 0)
                                continue;

                            if (res == 1 || res == 2 || !PromptEveryTime)
                            {
                                try
                                {
                                    fi.Delete();
                                    group.Dupes.Remove(fi.FullName);
                                }
                                catch (Exception fdex)
                                {
                                    DoOutput($"Error deleting file {fi.FullName}. Exception: {fdex.Message}", InternalLogOutputType.Error);
                                }
                            }

                            if (res == 2)
                                PromptEveryTime = false;

                            break;

                        case CleaningMethod.Recycle:
                            // Todo:
                            break;

                        case CleaningMethod.ReplaceWithLink:
                            // Todo:
                            break;

                        case CleaningMethod.ReplaceWithTextfile:
                            if (PromptEveryTime)
                                res = Prompt($"Replace file {fi.FullName} with a textfile ?", PromptType.AllYesNo);

                            if (res == 0)
                                continue;

                            if (res == 1 || res == 2 || !PromptEveryTime)
                            {
                                try
                                {
                                    fi.Delete();
                                    File.CreateText(fi.FullName + ".txt");
                                    group.Dupes.Remove(fi.FullName);
                                }
<<<<<<< HEAD
                                catch (Exception)
=======
                                catch (Exception rfex)
>>>>>>> ea5a6d1d8d5be10060e5749f0d04f1d83adef227
                                {
                                }
                            }

                            if (res == 2)
                                PromptEveryTime = false;

                            break;

                        default:
                            break;
                    } // switch

                } // foreach dupe

            }

            DoOutput("Done!", InternalLogOutputType.Green);
        }

        private void ExportResults()
        {
            string filename = this.Setup.OutputFilename;

            if (string.IsNullOrEmpty(filename))
                filename = "output";

            switch (this.Setup.ExportType)
            {
                case ExportType.JSON:
                    filename += ".json";
                    using (StreamWriter file = File.CreateText(filename))
                    {
                        JsonSerializer jsonserializer = new JsonSerializer();
                        jsonserializer.Serialize(file, this.DupeGroups);
                    }
                    break;

                case ExportType.XML:
                    filename += ".xml";

                    //// Nuget: Extended XML Serializer
                    //// IExtendedXmlSerializer serializer = new ConfigurationContainer().Create();
                    //IExtendedXmlSerializer serializer = new ConfigurationContainer().UseAutoFormatting()
                    //                                                                .UseOptimizedNamespaces()
                    //                                                                .EnableImplicitTyping(typeof(DupeGroupList))
                    //                                                                // Additional configurations...
                    //                                                                .Create();
                    //// string xml = serializer.Serialize(this.Dupes);
                    //var xml = serializer.Serialize(new XmlWriterSettings { Indent = true }, this.DupeGroups);
                    //File.WriteAllText(filename, xml);

                    XmlSerializer serializer = new XmlSerializer(typeof(DupeGroupList));
                    TextWriter writer = new StreamWriter(filename);
                    serializer.Serialize(writer, this.DupeGroups);
                    writer.Close();
                    break;

                case ExportType.TXT:
                    filename += ".txt";
                    StringBuilder sb = new StringBuilder();
                    foreach (DupeGroup g in this.DupeGroups)
                    {
                        sb.AppendLine($" Hash: {g.Hash}  [{BytesToString(g.Size)}] ");
                        foreach (string item in g.Dupes)
                            sb.AppendLine($"     {item}");
                    }                    
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename))
                        file.WriteLine(sb.ToString());

                    break;

                default:
                    break;
            }
        }

    }



}

// internal class ConsoleSpinner
//{
//    private int _currentAnimationFrame;

//    public ConsoleSpinner()
//    {
//        SpinnerAnimationFrames = new[]
//                                    {
//                                        '|',
//                                        '/',
//                                        '-',
//                                        '\\'
//                                    };
//    }

//    public char[] SpinnerAnimationFrames { get; set; }

//    public void UpdateProgress()
//    {
//        // Store the current position of the cursor
//        var originalX = Console.CursorLeft;
//        var originalY = Console.CursorTop;

//        // Write the next frame (character) in the spinner animation
//        Console.Write(SpinnerAnimationFrames[_currentAnimationFrame]);

//        // Keep looping around all the animation frames
//        _currentAnimationFrame++;
//        if (_currentAnimationFrame == SpinnerAnimationFrames.Length)
//        {
//            _currentAnimationFrame = 0;
//        }

//        // Restore cursor to original position
//        Console.SetCursorPosition(originalX, originalY);
//    }
//}
