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
        public DupeFileItemList DupeFileItemList { get; set; }

        public FileIndexItem()
        {
            this.DupeFileItemList = new DupeFileItemList();
        }
    }

    public class FileIndexItemList : List<FileIndexItem>
    {

        public FileIndexItemList()
        {            
        }

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
            this.Items = new FileIndexItemList();
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
                // DoOutput($"Index loaded with {this.Items.Count()} items.");

                if (this.Items == null)
                    {this.Items = new FileIndexItemList();}

            }
            catch (System.Exception ex)
            {
                // DoOutput($"Exception: {ex.Message}.");
                this.Items = new FileIndexItemList();

                // create a new index if the old is corrupt.
                DoOutput($"Could not load the index {filename}. Exception: {ex.Message}");
                // this.Items = new FileIndexItemList();

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

            int fc = 0;
            IEnumerable<FileInfo> list = EnumerateFilesRecursive(opt);            
            DoOutput($"Adding {list.Count()} files to the index.");

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

        // public string PrintByteArray(byte[] array)
        // {
        //     StringBuilder sb = new StringBuilder();
        //     for (int i = 0; i < array.Length; i++)
        //     {
        //         sb.Append($"{array[i]:X2}");
        //         if ((i % 4) == 3) sb.Append(" ");
        //     }
        //     return sb.ToString();
        // }

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
                        // DoOutput($"Calculating sha256 hash for {filename}.");
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
                        // DoOutput($"Calculating md5 hash for {filename}.");
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

        static bool BinaryCompareFiles(FileInfo first, FileInfo second)
        {
            int cnt = sizeof(Int64);

            // Commented out, because we check this before we call BinaryCompareFiles
            // if (first.Length != second.Length)
            //     return false;

            if (string.Equals(first.FullName, second.FullName, StringComparison.OrdinalIgnoreCase))
                return true;

            int iterations = (int)Math.Ceiling((double)first.Length / cnt);

            using (FileStream fs1 = first.OpenRead())
            using (FileStream fs2 = second.OpenRead())
            {
                byte[] one = new byte[cnt];
                byte[] two = new byte[cnt];
                for (int i = 0; i < iterations; i++)
                {
                    fs1.Read(one, 0, cnt);
                    fs2.Read(two, 0, cnt);

                    if (BitConverter.ToInt64(one, 0) != BitConverter.ToInt64(two, 0))
                        return false;
                }
            }
            return true;
        }

        public void Scan(ScanOptions opt)
        {
            // filter items
            var filterdItems = this.Items.Where(d => d.Size >= opt.MinSize && d.Size <= opt.MaxSize).ToList();

            if (filterdItems.Count() == 0)
            {
                DoOutput($"Nothing to scan, the index is empty. Please first add items to the index.");
                return;
            }

            DoOutput($"Scanning {filterdItems.Count} filtered items for binary duplicates.");

            // check items
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

                // DoOutput($"Found possible duplicate file(s) for: {item.FullFilename}");
                // foreach (FileIndexItem sub in sha256dupes)
                // {                          
                //     DoOutput($"- {sub.FullFilename}.");
                // }               
            }

            // Binary compare sha256 dupes
            int dupesfound = 0;
            IEnumerable<IGrouping<string, FileIndexItem>> possibledupes =
                this.Items.Where(t => t.HashSHA256 != null).GroupBy(f => f.HashSHA256, f => f);

            // Binary compare sha256 dupes
            foreach (IGrouping<string, FileIndexItem> g in possibledupes)
            {
                // DoOutput(String.Format("Hash: {0}", g.Key));
                foreach (FileIndexItem item in g)
                {
                    FileInfo file1 = new FileInfo(item.FullFilename);                  
                    var otherfiles = g.Where(t => t.HashSHA256 == g.Key && t.FullFilename != item.FullFilename);
                    foreach (var sub in otherfiles)
                    {
                        FileInfo file2 = new FileInfo(sub.FullFilename);
                        // DoOutput($"Binary comparing file {file1.FullName} with {file2.FullName}");                        
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