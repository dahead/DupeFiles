using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace dupefiles
{

    public class DupeFileItemList : List<DupeFileItem>
    {

    }

    public class DupeFileItem
    {
        public string FullFilename { get; set; }
    }

    public class FileIndexItem
    {
        public string FullFilename { get; set; }
        public string ShortName { get; set; }
        public long Size { get; set; }
        public string HashMD5  { get; set; }
        public string HashSHA256  { get; set; }


private DupeFileItemList _DupeFileItemList = new DupeFileItemList();
public DupeFileItemList DupeFileItemList
{
    get { return _DupeFileItemList; }
    set { _DupeFileItemList = value; }
}


        public FileIndexItem()
        {
            this.DupeFileItemList = new DupeFileItemList();
        }
    }

    public class FileIndexItemList : List<FileIndexItem>
    {

        public bool ContainsFileName(string filename)
        {
            foreach (FileIndexItem item in this)
            {
                if (item.FullFilename == filename)
                    return true;
            }
            return false;
        }

    }

    public class FileIndex
    {

        public SetupOptions Setup { get; set; }
        const string IndexFileName = "index.json";
        public FileIndexItemList Items { get; set; }
        public StringBuilder LogFile { get; set; }

        public FileIndex()
        {
            this.Setup = new SetupOptions();
            this.Items = new FileIndexItemList();
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
                    this.Items = new FileIndexItemList();
                    this.Items = (FileIndexItemList)serializer.Deserialize(file, typeof(FileIndexItemList));
                }           
                DoOutput($"Index loaded with {this.Items.Count()} items.");
            }
            catch (System.Exception ex)
            {
                DoOutput($"Exception: {ex.Message}.");
            }

        }

        public void SaveIndexAs(string filename)
        {
            // serialize JSON directly to a file
            using (StreamWriter file = File.CreateText(filename))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, this.Items);
            }
            DoOutput($"Index saved with {this.Items.Count()} items as {filename}.");
        }

        public void Load()
        {
            if (System.IO.File.Exists(IndexFileName))
            {
                this.LoadIndexFrom(IndexFileName);
            }               
            else
            {
                this.Items = new FileIndexItemList();
            }
        }

        public void Save()
        {
            this.SaveIndexAs(IndexFileName);
        }

        public int DoSetup(SetupOptions opt)
        {
            this.Setup = opt;
            return 0;
        }

        public void DoOutput(string output)
        {
            switch (this.Setup.OutputType)
            {
                case OutputType.Console:
                    Console.WriteLine(output);
                    break;
                
                case OutputType.LogFile:
                    this.LogFile.AppendLine(output);
                    break;

                case OutputType.XML:
                    // todo...
                    break;
            }
        }

        public int AddDirectory(AddOptions opt)
        {
            DirectoryInfo basedi = null;
            try
            {
                basedi = new DirectoryInfo(opt.Path);
            }
            catch (System.Exception ex)
            {
                DoOutput("Exception: " + ex.Message);
                return 0;
            }

            int fc = 0;
            IEnumerable<FileInfo> list = EnumerateFilesRecursive(opt);            
            DoOutput($"Adding {list.Count()} items.");

            foreach (FileInfo fi in list)
            {
                // Todo: check if file is alread in index and update it.
                if (this.Items.ContainsFileName(fi.FullName))
                {
                    // DoOutput($"Skipping file {fi.FullName}. Already in index.");
                    continue;
                }

                // Create new File Index Item
                FileIndexItem newitem = new FileIndexItem() 
                {
                        FullFilename = fi.FullName, 
                        ShortName = fi.Name,
                        Size = fi.Length,
                        HashMD5  = CalculateMD5(fi.FullName),
                        // HashSHA256 = CalculateSHA256(fi),
                };

                // Add new item to index
                DoOutput($"Adding {newitem.HashMD5} file {fi.FullName})");
                // DoOutput($"Adding file {fi.FullName} to the index.)");
                this.Items.Add(newitem);
                fc +=1;
            }

            DoOutput($"Added directory {basedi.FullName} with {fc} items.)");
            return fc;
        }

        public static void PrintByteArray(byte[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                Console.Write($"{array[i]:X2}");
                if ((i % 4) == 3) Console.Write(" ");
            }
            Console.WriteLine();
        }

        static IEnumerable<FileInfo> TraverseDirectory(string rootPath, Func<FileInfo, bool> Pattern)
        {
            var directoryStack = new Stack<DirectoryInfo>();
            directoryStack.Push(new DirectoryInfo(rootPath));
            while (directoryStack.Count > 0)
            {
                var dir = directoryStack.Pop();
                try
                {
                    foreach (var subdir in dir.GetDirectories())
                        directoryStack.Push(subdir);
                }
                catch (UnauthorizedAccessException) {
                    continue; // We don't have access to this directory, so skip it
                }
                foreach (var f in dir.GetFiles().Where(Pattern)) // "Pattern" is a function
                    yield return f;
            }
        }

        public static IEnumerable<FileInfo> DirectoryDownTheRabbitHole(string rootPath, bool Recursive, string Pattern)
        {
            int max = 0;

            IEnumerable<string> EnumeratedDir = Enumerable.Empty<string>();
            Queue<Exception> exceptions = new Queue<Exception>();
            Queue<string> pending = new Queue<string>();

            pending.Enqueue(rootPath);

            while (pending.Count > 0)
            {
                try
                {
                    EnumeratedDir = Directory.EnumerateDirectories(pending.Dequeue(), Pattern, SearchOption.TopDirectoryOnly);
                }
                catch (Exception e)
                {
                    exceptions.Enqueue(e);
                    continue; // skip this directory
                }

                while (exceptions.Count > 0)
                {
                    // TODO: switch on the throwing if PowerShell is consumer
                    //throw exceptions.Dequeue();
                    var nothing = exceptions.Dequeue();
                }

                if (EnumeratedDir != null)
                {
                    foreach (string returnedDir in EnumeratedDir)
                    {                    
                        foreach (var f in new DirectoryInfo(returnedDir).GetFiles(Pattern))
                            yield return f;

                        if (Recursive)
                        {
                            pending.Enqueue(returnedDir);
                        }
                    }
                }

                if (pending.Count > max)
                {
                    max = pending.Count;
                }
            }
        }

        public IEnumerable<FileInfo> EnumerateFilesRecursive(AddOptions opt)
        {
            var todo = new Queue<string>();
            todo.Enqueue(opt.Path);
            while (todo.Count > 0)
            {
                string dir = todo.Dequeue();
                string[] subdirs = new string[0];
                string[] files = new string[0];

                DirectoryInfo di = new DirectoryInfo(dir);

                if (opt.SkipDirectoriesStartingWithADot)
                    if (di.Name.StartsWith("."))
                        continue;

                try
                {
                    if (opt.Recursive)
                        subdirs = Directory.GetDirectories(dir);
                    else
                    {
                        subdirs = new string[] { dir };
                    }

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

                    if (opt.SkipDirectoriesStartingWithADot)
                        if (subdir.StartsWith("."))
                            continue;

                    try
                    {
                        // DirectoryInfo 
                        // di = new DirectoryInfo(subdir);
                        // if (di.Exists)
                            todo.Enqueue(subdir);
                    }
                    catch (System.Exception ex)                    
                    {                   
                        DoOutput($"Exception: {ex.Message}");                        
                        continue;
                    }
                }                   

                try
                {
                    files = Directory.GetFiles(dir, opt.Pattern);
                }                
                catch (IOException ex)
                {
                    DoOutput($"IO Exception: {ex.Message}");
                    continue;
                }
                catch (System.UnauthorizedAccessException ex)
                {
                    DoOutput($"UnauthorizedAccess Exception: {ex.Message}");
                    continue;
                }
                foreach (string filename in files)
                {
                    yield return new FileInfo(filename);
                }
             
            }
        }

        static string CalculateSHA256(string filename)
        {
            if (!System.IO.File.Exists(filename))
                {return string.Empty;}
            try
            {
                using (var mySHA256 = SHA256.Create())
                {
                    using (var stream = File.OpenRead(filename))
                    {
                        // Console.WriteLine($"Calculating sha256 hash for {filename}.");
                        var hash = mySHA256.ComputeHash(stream);                    
                        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }
                }                    
            }
            catch (System.Exception ex)
            {
                // DoOutput($"Exception when creating sha256 hash: {ex.Message}.");
                return string.Empty;
            }                   
        }

        static string CalculateMD5(string filename)
        {
            if (!System.IO.File.Exists(filename))
                {return string.Empty;}
            try
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(filename))
                    {
                        // Console.WriteLine($"Calculating md5 hash for {filename}.");
                        var hash = md5.ComputeHash(stream);
                        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }
                }                
            }
            catch (System.Exception ex)
            {
                // DoOutput($"Exception when creating MD5 hash: {ex.Message}.");
                return string.Empty;
            }
        }

        const int BinaryCompareFiles_BytesToRead = sizeof(Int64);

        static bool BinaryCompareFiles(FileInfo first, FileInfo second)
        {
            if (first.Length != second.Length)
                return false;

            if (string.Equals(first.FullName, second.FullName, StringComparison.OrdinalIgnoreCase))
                return true;

            int iterations = (int)Math.Ceiling((double)first.Length / BinaryCompareFiles_BytesToRead);

            using (FileStream fs1 = first.OpenRead())
            using (FileStream fs2 = second.OpenRead())
            {
                byte[] one = new byte[BinaryCompareFiles_BytesToRead];
                byte[] two = new byte[BinaryCompareFiles_BytesToRead];

                for (int i = 0; i < iterations; i++)
                {
                    fs1.Read(one, 0, BinaryCompareFiles_BytesToRead);
                    fs2.Read(two, 0, BinaryCompareFiles_BytesToRead);

                    if (BitConverter.ToInt64(one,0) != BitConverter.ToInt64(two,0))
                        return false;
                }
            }
            return true;
        }

        public void Scan(ScanOptions opt)
        {

            var filterdItems = this.Items.Where(d => d.Size >= opt.MinSize).ToList();
            DoOutput($"Scanning {filterdItems.Count} filtered items.");            
            FileIndexItemList dupes = new FileIndexItemList();

            foreach (FileIndexItem item in filterdItems)
            {
                // check size dupes and then calculate md5 hash
                var sizedupes = filterdItems.Where(d => d.Size == item.Size && d.FullFilename != item.FullFilename).ToList();
                if (sizedupes.Count == 0)
                    continue;                    
                foreach (FileIndexItem sub in sizedupes)
                {                          
                    sub.HashMD5 = CalculateMD5(sub.FullFilename); 
                }

                // check md5 dupes and then calculate sha256 hash
                var md5dupes = sizedupes.Where(d => d.HashMD5 == item.HashMD5).ToList();
                if (md5dupes.Count == 0)
                    continue;
                foreach (FileIndexItem sub in sizedupes)
                {      
                    sub.HashSHA256 = CalculateSHA256(sub.FullFilename);
                }

                 // check sha256 dupes and then compare binary
                var sha256dupes = md5dupes.Where(d => d.HashSHA256 == item.HashSHA256).ToList();
                if (sha256dupes.Count == 0)
                    continue;
                
                DoOutput($"Found possible duplicate files for: {item.FullFilename}.");
                foreach (FileIndexItem sub in sha256dupes)
                {                          
                    DoOutput($"    Found possible duplicate file: {sub.FullFilename}.");
                }               
            }


            // Binary compare sha256 dupes
            int dupesfound = 0;
            IEnumerable<IGrouping<string, FileIndexItem>> possibledupes =
                this.Items.Where(t => t.HashSHA256 != null).GroupBy(f => f.HashSHA256, f => f);

            foreach (IGrouping<string, FileIndexItem> g in possibledupes)
            {
                // Print the key value of the IGrouping.
                DoOutput(String.Format("Hash: {0}", g.Key));

                foreach (FileIndexItem item in g)
                {
                    FileInfo file1 = new FileInfo(item.FullFilename);
                    
                    var otherfiles = g.Where(t => t.HashSHA256 == g.Key && t.FullFilename != item.FullFilename);

                    foreach (var sub in otherfiles)
                    {
                        FileInfo file2 = new FileInfo(sub.FullFilename);
                        DoOutput($"Binary comparing files {file1.FullName} with {file2.FullName}.");                        
                        bool identical = BinaryCompareFiles(file1, file2);
                        // Dupe found
                        if (identical)
                        {
                            // todo: check if list already contains item
                            DoOutput($"Duplicate file found: {file2.FullName}");
                            item.DupeFileItemList.Add(new DupeFileItem() { FullFilename = file2.FullName} );
                            dupesfound += 1;
                        }
                            
                    }

                }                   
            }

            // Finished.
            DoOutput($"Scan finished. Possible dupes {dupesfound}.");
        }

        public void Info(IndexInfoOptions opt)
        {

            if (this.Items.Count() == 0)
                {return;}

            // Show duplicate files by sha256 hash and where DupeFileItemList is not empty
            IEnumerable<IGrouping<string, FileIndexItem>> dupes =
                    this.Items.Where(t => t.HashSHA256 != null).Where(t => t.DupeFileItemList != null).GroupBy(f => f.HashSHA256, f => f);
            foreach (IGrouping<string, FileIndexItem> g in dupes)
            {
                // Print the key value of the IGrouping.
                DoOutput(String.Format("Duplicate files with hash: {0}", g.Key));
                // Iterate over each value in the 
                // IGrouping and print the value.
                foreach (FileIndexItem item in g)
                    DoOutput(String.Format("     {0}", item.FullFilename));
            }

        }

        public void Purge(PurgeOptions opt)
        {
            // remove dead files from the index
            int counter = 0;
            for (int i = this.Items.Count - 1; i >= 0; i--)
            {
                FileIndexItem itm = this.Items[i];
                FileInfo fi = new FileInfo(itm.FullFilename);
                if (!fi.Exists)
                {
                    counter += 1;
                    this.Items.RemoveAt(i);
                }
                // check dupe list for dead files
                for (int f = itm.DupeFileItemList.Count() - 1; f >= 0; f--)
                {
                    DupeFileItem subitm = itm.DupeFileItemList[f];
                    fi = new FileInfo(subitm.FullFilename);
                    if (!fi.Exists)
                    {
                        itm.DupeFileItemList.RemoveAt(f);
                    }
                }
            }
            this.DoOutput($"Purged {counter} files from the index.");
        }

    }

}