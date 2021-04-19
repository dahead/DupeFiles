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
using ExtendedXmlSerializer;
using ExtendedXmlSerializer.Configuration;

namespace dupefiles
{

    public enum InternalLogOutputType { Info, Error, Red, Green, Yellow, Blue }

    public class FileIndexItem
    {
        public string FullFilename { get; set; }
        public long Size { get; set; }
        public string Hash  { get; set; }
    }

    public class FileIndexItemList : List<FileIndexItem>
    {
    }

    public class FileIndexItemDictonary : Dictionary<string, FileIndexItem>
    {
    }

    public class FileIndex
    {

        public SetupOptions Setup { get; set; }
        const string IndexFileName = "index.json";
        public FileIndexItemDictonary Items { get; set; }
        public FileIndexItemList Dupes { get; set; }
        public IEnumerable<IGrouping<string, FileIndexItem>> DupesGroupedByHash { get => GetDupesGroupedByHash(); }

        private IEnumerable<IGrouping<string, FileIndexItem>> GetDupesGroupedByHash()
        {
            // return this.Dupes.GroupBy(f => f.Hash, f => f);
            return this.Dupes.OrderByDescending(p => p.Size).GroupBy(f => f.Hash, f => f);
        }

        public StringBuilder LogFile { get; set; }
        public bool Cancel { get; set; }

        public FileIndex()
        {
            this.Items = new FileIndexItemDictonary();
            this.Dupes = new FileIndexItemList();
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
                }           
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
 
        public void Load()
        {
            if (System.IO.File.Exists(IndexFileName))
                this.LoadIndexFrom(IndexFileName);
            else
                DoOutput($"Index not found: {IndexFileName}.");
        }

        public int LoadSetup(string filename = "")
        {
            // no configuration filename given
            if (string.IsNullOrEmpty(filename))
                filename = GetSetupFilename();

            if (!System.IO.File.Exists(filename))
                return -1;

            // deserialize JSON directly from a file
            using (StreamReader file = File.OpenText(filename))
            {
                JsonSerializer serializer = new JsonSerializer();
                SetupOptions opt = (SetupOptions)serializer.Deserialize(file, typeof(SetupOptions));

                // nothing loaded? Create default setup.
                if (opt == null)
                    opt = new SetupOptions();
            
                // apply setup            
                this.Setup = opt;
                // DoOutput($"Loaded setup from file {filename}.");
            }
            return 0;
        }

        public int SaveSetup(SetupOptions opt, string filename = "")
        {

            if (string.IsNullOrEmpty(filename))
                filename = GetSetupFilename();

            // remember setup
            this.Setup = opt;

            // serialize JSON directly to a file
            using (StreamWriter file = File.CreateText(filename))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, opt);
            }

            DoOutput($"Saved setup to file {filename}.");

            return 0;
        }

        internal void Analyze(AnalyticsOptions opt)
        {
            // create html file with balken diagramms
            throw new NotImplementedException();
        }

        internal void Clean(CleanOptions opt)
        {
            foreach (var item in this.Dupes)
            {
                FileInfo fi = new FileInfo(item.FullFilename);
                switch (opt.Method)
                {
                    case CleaningMethod.MoveDupesToNewLocation:
                        string newfn = System.IO.Path.Combine(opt.MoveToDirectory, fi.Name);
                        fi.MoveTo(newfn);
                        break;

                    case CleaningMethod.DeleteDupes:
                        fi.Delete();
                        break;

                    case CleaningMethod.MoveDupesToRecycleBin:
                        // Todo:
                        // File.Move()
                        break;

                    case CleaningMethod.ReplaceDupesWithLink:
                        // Todo:
                        break;

                    case CleaningMethod.ReplaceDupesWithTextfile:
                        fi.Delete();
                        File.CreateText(fi.FullName + ".txt");
                        break;
                    case CleaningMethod.DeleteAllWhichMatchFilter:
                        if (!string.IsNullOrEmpty(opt.Filter))
                        {
                            if (fi.FullName.Contains(opt.Filter))
                                fi.Delete();
                        }
                        break;
                    default:
                        break;
                }

            }
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

        private void ExportFoundDupesToFile()
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
                        jsonserializer.Serialize(file, this.DupesGroupedByHash);
                    }
                    break;

                case ExportType.XML:
                    filename += ".xml";

                    // Nuget: Extended XML Serializer
                    // IExtendedXmlSerializer serializer = new ConfigurationContainer().Create();
                    IExtendedXmlSerializer serializer = new ConfigurationContainer().UseAutoFormatting()
                                                                                    .UseOptimizedNamespaces()
                                                                                    .EnableImplicitTyping(typeof(IEnumerable<IGrouping<string, FileIndexItem>>))
                                                                                    // Additional configurations...
                                                                                    .Create();

                    // string xml = serializer.Serialize(this.Dupes);
                    var xml = serializer.Serialize(new XmlWriterSettings { Indent = true }, this.Dupes);
                    File.WriteAllText(filename, xml);

                    //XmlSerializer serializer = new XmlSerializer(typeof(IEnumerable<IGrouping<string, FileIndexItem>>));
                    //TextWriter writer = new StreamWriter(filename);
                    //serializer.Serialize(writer, this.DupesGroupedByHash);
                    //writer.Close();
                    break;

                case ExportType.TXT:
                    filename += ".txt";
                    StringBuilder sb = new StringBuilder();
                    foreach (var d in this.DupesGroupedByHash)
                        sb.AppendLine(d.Key);
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename))
                        file.WriteLine(sb.ToString());
                    break;

                default:
                 break;
	        }
        }

        private string GetSetupFilename()
        {
            // Use DoNotVerify in case LocalApplicationData doesn’t exist.
            string appData = Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData,System.Environment.SpecialFolderOption.DoNotVerify), "DupeFiles");
        
            // Ensure the directory and all its parents exist.
            DirectoryInfo di = Directory.CreateDirectory(appData);
            string result = System.IO.Path.Combine(di.FullName, "config.json");

            // create config file if it does not exist yet
            if (!System.IO.File.Exists(result))
            {
                FileStream fs = File.Create(result);
                fs.Close();                
            }
            return result;
        }       

        public void DoOutput(string output = "", InternalLogOutputType t = InternalLogOutputType.Info)
        {
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

        public int AddDirectory(AddOptions opt)
        {
            DirectoryInfo basedi = null;
            try
            {
                basedi = new DirectoryInfo(opt.Path);
                if (!basedi.Exists)
                    { 
                        DoOutput($"Error! Directory does not exist {opt.Path}!", InternalLogOutputType.Error);                        
                        return 0; 
                    }
            }
            catch (System.Exception ex)
            {
                DoOutput($"Exception: {ex.Message}", InternalLogOutputType.Error);
                return 0;
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
                catch (System.ArgumentException argex)
                {
                    // DoOutput($"Exception: {argex.Message}", InternalLogOutputType.Error);
                }
                catch (System.Exception ex)
                {
                    DoOutput($"Exception: {ex.Message}", InternalLogOutputType.Error);
                }

                // log every 100th new file we add to the output so we see something
                if (fc % 100 == 0 && fc != 0)
                    DoOutput($"- {fi.FullName}");

                // Cancel?
                if (this.Cancel)
                    return -1;

            }
            // Done
            DoOutput($"Added directory {opt.Path} with {fc} items.", InternalLogOutputType.Green);

            // Statistics
            sw.Stop();
            TimeSpan ts = sw.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            this.DoOutput("Adding items took " + elapsedTime);

            // Return
            return fc;
        }

        public int Remove(RemoveOptions opt)
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
            return fc;
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
                using (var mySHA256 = SHA256.Create())
                {
                    using (var stream = File.OpenRead(filename))
                    {
                        // DoOutput($"Calculating sha256 hash for {filename}.");
                        var hash = mySHA256.ComputeHash(stream);                    
                        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }
                }                    
            }
            catch (System.Exception ex)            
            {
                // DoOutput($"Exception when creating sha256 hash: {ex.Message}.");
                // return ex.Message;
                return "ErrorHash";
            }                   
        }

        //private string CalculateMD5(string filename)
        //{
        //    if (!System.IO.File.Exists(filename))
        //        return string.Empty;
        //    try
        //    {
        //        using (var md5 = MD5.Create())
        //        {
        //            using (var stream = File.OpenRead(filename))
        //            {
        //                // DoOutput($"Calculating md5 hash for {filename}.");
        //                var hash = md5.ComputeHash(stream);
        //                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        //            }
        //        }                
        //    }
        //    catch (System.Exception ex)
        //    {
        //        // DoOutput($"Exception when creating MD5 hash: {ex.Message}.");
        //        return ex.Message;
        //    }
        //}

        //public static bool AreFileContentsEqual(FileInfo fi1, FileInfo fi2) =>
        //    fi1.Length == fi2.Length &&
        //    (fi1.Length == 0 || File.ReadAllBytes(fi1.FullName).SequenceEqual(
        //                        File.ReadAllBytes(fi2.FullName)));
        
        private static bool StreamsContentsAreEqual(FileInfo file1, FileInfo file2)
        {
            const int bufferSize = 2048 * 2;
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

        public int Scan(ScanOptions opt)
        {
            if (Cancel)
                return -1;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            // clear list
            this.Dupes.Clear();

            // filter items
            var filterdItems = this.Items.Where(d => d.Value.Size >= opt.MinSize && d.Value.Size <= opt.MaxSize).ToList();

            // is there anything to scan?
            if (filterdItems.Count() == 0)
            {
                DoOutput($"Nothing to scan, the index is empty. Please add items to the index first by using the command 'idplus'.");
                return 0;
            }

            // Get all file size duplicates without comparing with our self (t.key)
            DoOutput($"Starting base scan on {filterdItems.Count} filtered items...");
            IEnumerable<IGrouping<long, KeyValuePair<string, FileIndexItem>>> fsd =
                filterdItems.GroupBy(f => f.Value.Size, f => f);

            DoOutput("Done.", InternalLogOutputType.Green);

            DoOutput($"Calculating hashes for ~{fsd.Count()} file size duplicates...");
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
                            var x = Task.Run(() => CalculateSHA256(sub.Value.FullFilename));                            
                            do
                            {
                                // output some progress
                                // Console.Write(".");
                            } while (!x.IsCompleted);
                            // remember hash
                            this.Items[sub.Value.FullFilename].Hash = x.Result;
                        }
                    }
                }

                // Cancel?
                if (this.Cancel)
                    return -1;
            }
            DoOutput(Environment.NewLine + "Done.", InternalLogOutputType.Green);

            // Binary compare sha256 dupes
            int counter = 1;
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
                                {
                                    try
                                    {
                                        if (!this.Dupes.Contains(subitem.Value))
                                        {
                                            this.Dupes.Add(subitem.Value);
                                            dupesfound += 1;
                                        }
                                    }
                                    catch (System.Exception ex)
                                    {
                                        // DoOutput($" Exception: {ex.Message}", InternalLogOutputType.Red);
                                    }
                                }
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
                if (this.Cancel)
                    return -1;
            }
            DoOutput("Done.", InternalLogOutputType.Green);

            // Option: save output to file...
            if (!string.IsNullOrEmpty(this.Setup.OutputFilename))
                this.ExportFoundDupesToFile();

            // Finished.
            long totalsize = this.Dupes.Sum(item => item.Size);
            DoOutput($"Found a total of {dupesfound} duplicates files with a size of {BytesToString(totalsize)}:");

            // Statistics
            sw.Stop();
            TimeSpan ts = sw.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            DoOutput("Scan took " + elapsedTime);

            // Output results
            if (this.Setup.OutputType != OutputType.Silent)
                this.ShowDupes();

            // Save some stuff
            try
            {
                // Save the index
                // since we dont want to change the Index in the loop
                // we currently dont save it
                // this.SaveIndex();

            }
            catch (Exception ex)
            {
                DoOutput(ex.Message, InternalLogOutputType.Error);
            }


            // Finally return the amount of found duplicates
            return dupesfound;
        }

        private void ShowDupes()
        {
            var dupes = this.Dupes.OrderByDescending(p => p.Size).GroupBy(f => f.Hash, f => f);
            foreach (var g in dupes)
            {
                if (g.Count() > 1)
                {
                    DoOutput($" Hash: {g.Key}", InternalLogOutputType.Yellow);
                    foreach (var item in g)
                        DoOutput($"     {item.FullFilename} [{BytesToString(item.Size)}]");
                }
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

        public void PurgeIndex(PurgeOptions opt)
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
