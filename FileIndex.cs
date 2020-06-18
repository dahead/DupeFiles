using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Threading;

namespace dupefiles
{

    public class FileIndexItem
    {
        public string FullFilename { get; set; }
        public long Size { get; set; }
        public string Hash  { get; set; }
    }

    public class FileIndexItemList : Dictionary<string, FileIndexItem>
    {
    }

    public class FileIndex
    {

        public SetupOptions Setup { get; set; }
        const string IndexFileName = "index.json";
        public FileIndexItemList Items { get; set; }
        public FileIndexItemList Dupes { get; set; }
        public StringBuilder LogFile { get; set; }

        public FileIndex()
        {
            this.Items = new FileIndexItemList();
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
                    this.Items = (FileIndexItemList)serializer.Deserialize(file, typeof(FileIndexItemList));
                }           
            }
            catch (System.Exception ex)
            {
                DoOutput($"Could not load the index {filename}. Exception: {ex.Message}");
            }
        }

        public void Save()
        {
            this.SaveIndexAs(IndexFileName);
        }

        public void SaveIndexAs(string filename)
        {
            // serialize JSON directly to a file
            using (StreamWriter file = File.CreateText(filename))
            {
                JsonSerializer serializer = new JsonSerializer() { Formatting = Formatting.Indented };
                serializer.Serialize(file, this.Items);
            }
            // DoOutput($"Index saved with {this.Items.Count()} items as {filename}.");
        }

        public void Load()
        {
            if (System.IO.File.Exists(IndexFileName))
            {
                this.LoadIndexFrom(IndexFileName);
            }
            else
            {
                DoOutput($"Index not found: {IndexFileName}.");
            }
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

        private void SaveDupes(string filename)
        {
            // serialize JSON directly to a file
            using (StreamWriter file = File.CreateText(filename))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, this.Dupes);
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

        public void DoOutput(string output = "")
        {
            switch (this.Setup.OutputType)
            {
                case OutputType.Console:
                    Console.WriteLine(output);
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
                        DoOutput($"Error! Directory does not exist {opt.Path}!");                        
                        return 0; 
                    }
            }
            catch (System.Exception ex)
            {
                DoOutput($"Exception: {ex.Message}");
                return 0;
            }

            DoOutput($"Adding content of {opt.Path} to the index. Please stand by...");

            // for each file
            int fc = 0;
            IEnumerable<FileInfo> list = EnumerateFilesRecursive(opt); //.Where(t => t.Exists == true);
            foreach (FileInfo fi in list)
            {
                try
                {
                    // Create new File Index Item
                    FileIndexItem newitem = new FileIndexItem() 
                    {
                            FullFilename = fi.FullName, 
                            Size = fi.Length,
                    };
                    // Add new item to index
                    this.Items.Add(newitem.FullFilename, newitem);
                    fc +=1;                   
                }
                catch (System.ArgumentException)
                {
                }

                // log every 100th new file we add to the output so we see something
                if (fc % 100 == 0 && fc != 0)
                {
                    DoOutput($"- Adding file to index: {fi.FullName}");
                }

            }
            DoOutput($"Added directory {opt.Path} with {fc} items.");
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
            while (todo.Count > 0)
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
                    this.DoOutput($"IO Exception: {ex.Message}");                    
                    continue;
                }
                catch (System.UnauthorizedAccessException ex)
                {
                    DoOutput($"UnauthorizedAccess Exception: {ex.Message}");
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
                return ex.Message;
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
                    using (var stream = File.OpenRead(filename))
                    {
                        // DoOutput($"Calculating md5 hash for {filename}.");
                        var hash = md5.ComputeHash(stream);
                        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }
                }                
            }
            catch (System.Exception ex)
            {
                // DoOutput($"Exception when creating MD5 hash: {ex.Message}.");
                return ex.Message;
            }
        }

        public static bool AreFileContentsEqual(FileInfo fi1, FileInfo fi2) =>
            fi1.Length == fi2.Length &&
            (fi1.Length == 0 || File.ReadAllBytes(fi1.FullName).SequenceEqual(
                                File.ReadAllBytes(fi2.FullName)));
        
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
            // filter items
            var filterdItems = this.Items.Where(d => d.Value.Size >= opt.MinSize && d.Value.Size <= opt.MaxSize).ToList();

            // is there anything to scan?
            if (filterdItems.Count() == 0)
            {
                DoOutput($"Nothing to scan, the index is empty. Please add items to the index first by using the command 'idplus'.");
                return 0;
            }

            // Get all file size duplicates without comparing with our self (t.key)
            var s = new ConsoleSpinner();
            ConsoleColor before = Console.ForegroundColor;
            DoOutput($"Starting base scan on {filterdItems.Count} filtered items...");
            IEnumerable<IGrouping<long, KeyValuePair<string, FileIndexItem>>> fsd =
                filterdItems.GroupBy(f => f.Value.Size, f => f);

            Console.ForegroundColor = ConsoleColor.Green;
            DoOutput("Done.");
            Console.ForegroundColor = before;           

            DoOutput($"Calculating hashes for ~{fsd.Count()} file size duplicates...");
            foreach (IGrouping<long, KeyValuePair<string, FileIndexItem>> g in fsd)
            {
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
                                x.Wait(1000);
                                s.UpdateProgress();
                            } while (!x.IsCompleted);

                             this.Items[sub.Value.FullFilename].Hash = x.Result;
                            //  DoOutput($"Calculated hash: {this.Items[sub.Value.FullFilename].Hash}");
                            //  DoOutput($"    for file {this.Items[sub.Value.FullFilename].FullFilename}");
                        }
                    }
                    s.UpdateProgress();
                }
                s.UpdateProgress();
            }

            Console.ForegroundColor = ConsoleColor.Green;
            DoOutput("Done.");
            Console.ForegroundColor = before;           

            // Binary compare sha256 dupes
            int dupesfound = 0;
            IEnumerable<IGrouping<string, KeyValuePair<string, FileIndexItem>>> possibledupes =
                this.Items.Where(t => t.Value.Hash != null).GroupBy(f => f.Value.Hash, f => f);

            // Binary compare sha256 dupes
            DoOutput($"Found {possibledupes.Count()} possible hash groups for binary comparism...");
            foreach (IGrouping<string, KeyValuePair<string, FileIndexItem>> g in possibledupes)
            {
                if (g.Count() > 1)
                {
                    DoOutput($"Staring binary comparism on hash group {g.Key}");
                    foreach (KeyValuePair<string, FileIndexItem> item in g)
                    {
                        FileInfo file1 = new FileInfo(item.Value.FullFilename);
                        if (!file1.Exists)
                        {
                            this.Items.Remove(item.Key);
                            continue;
                        }

                        var otherfiles = g.Where(t => t.Value.Hash == g.Key && t.Value.FullFilename != item.Value.FullFilename);
                        foreach (var sub in otherfiles)
                        {
                            FileInfo file2 = new FileInfo(sub.Value.FullFilename);
                            if (!file2.Exists)
                            {
                                this.Items.Remove(sub.Key);
                                continue;
                            }                            
                            bool identical = false;
                            try
                            {                       
                                identical = StreamsContentsAreEqual(file1, file2);
                                // Dupe found
                                if (identical)
                                {
                                    // ConsoleColor before = Console.ForegroundColor;

                                    // Console.ForegroundColor = ConsoleColor.Red;
                                    // DoOutput($" Duplicate file pair found:");
                                    // Console.ForegroundColor = before;

                                    // DoOutput($"     {file1.FullName}");                                    
                                    // DoOutput($"     {file2.FullName}");
                                    
                                    try
                                    {
                                        this.Dupes.Add(sub.Value.FullFilename, sub.Value);
                                        dupesfound += 1;                                        
                                    }
                                    catch (System.Exception)
                                    {
                                    }
                                }
                            }
                            catch (System.Exception)
                            {
                                // DoOutput($"Error comparing files {file1.Name} and {file2.Name}. Error: {ex.Message}.");
                            }
                            s.UpdateProgress();
                        }
                        s.UpdateProgress();
                    }
                }
                s.UpdateProgress();
            }

            Console.ForegroundColor = ConsoleColor.Green;
            DoOutput("Done.");
            Console.ForegroundColor = before;      

            // Finished.
            long totalsize = this.Dupes.Sum(item => item.Value.Size);
            DoOutput($"Found a total of {dupesfound} duplicates files with a size of {BytesToString(totalsize)}:");

            // var query = from f in this.Dupes
            //             group f by new {f.Value.FullFilename, f.Value.Size, f.Value.Hash} into g
            //             select g.OrderByDescending(e => e.Value.Size);
            

            IEnumerable<IGrouping<string, KeyValuePair<string, FileIndexItem>>> dupes =
                this.Dupes.OrderByDescending(p => p.Value.Size).GroupBy(f => f.Value.Hash, f => f);

            foreach (IGrouping<string, KeyValuePair<string, FileIndexItem>> g in dupes)
            {
                if (g.Count() > 1)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    DoOutput($" Hash: {g.Key}");
                    Console.ForegroundColor = before;

                    foreach (KeyValuePair<string, FileIndexItem> item in g)
                    {
                        DoOutput($"     {item.Value.FullFilename} [{BytesToString(item.Value.Size)}]");                        
                    }
                }
            }

            // Option: save output to file...
            if (!string.IsNullOrEmpty(opt.Output))
            {
                this.SaveDupes(opt.Output);
            }

            this.Save();
            return dupesfound;
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

        public void Purge(PurgeOptions opt)
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

 internal class ConsoleSpinner
{
    private int _currentAnimationFrame;

    public ConsoleSpinner()
    {
        SpinnerAnimationFrames = new[]
                                    {
                                        '|',
                                        '/',
                                        '-',
                                        '\\'
                                    };
    }

    public char[] SpinnerAnimationFrames { get; set; }

    public void UpdateProgress()
    {
        // Store the current position of the cursor
        var originalX = Console.CursorLeft;
        var originalY = Console.CursorTop;

        // Write the next frame (character) in the spinner animation
        Console.Write(SpinnerAnimationFrames[_currentAnimationFrame]);

        // Keep looping around all the animation frames
        _currentAnimationFrame++;
        if (_currentAnimationFrame == SpinnerAnimationFrames.Length)
        {
            _currentAnimationFrame = 0;
        }

        // Restore cursor to original position
        Console.SetCursorPosition(originalX, originalY);
    }
}
