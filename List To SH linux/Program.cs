using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace List_To_SH_linux
{
    class Program
    {
        public static readonly string FolderSprator;
        public static int c = 1;
        public static string command = "";
        static Program()
        {
            var os = Environment.OSVersion;
            if (os.Platform == PlatformID.Win32NT)
                FolderSprator = "\\";
            else
                FolderSprator = "/";
        }
        static void Main(string[] args)
        {
            var argslist = args.ToList();
            var path = "";
            if (args.Contains("-c"))
            {
                var index = argslist.IndexOf("-c") + 1;
                if (args.Length > index)
                    int.TryParse(args[index], out c);
                argslist.RemoveAt(index);
                argslist.Remove("-c");
            }
            if (argslist.Count > 1)
            {
                command = argslist[0];
                path = argslist[1];
            }
            else
            {
                Console.WriteLine("Pleas enter the path");
                path = Console.ReadLine();
                Console.WriteLine("Pleas enter the command");
                command = Console.ReadLine();
            }
            Serach(new DirectoryInfo(path));
        }
        static void Serach(DirectoryInfo directory)
        {
            foreach (var item in directory.GetFiles())
                if (item.Name.Contains("link.json"))
                    Do(item);

            foreach (var item in directory.GetDirectories())
                Serach(item);
        }
        static void Do(FileInfo file)
        {
            try
            {
                var sr = new StreamReader(file.FullName);
                var list = JsonConvert.DeserializeObject<List<List<string>>>(sr.ReadToEnd());

                var all = new List<string>();
                foreach (var item in list)
                {
                    var fileName = item[0];
                    item.Remove(fileName);
                    foreach (var item2 in item)
                        all.Add(command.Replace("<FileName>", fileName).Replace("<Link>", item2));
                }

                var sw = new List<StreamWriter>();
                for (int i = 0; i < c; i++)
                    sw.Add(new StreamWriter(file.FullName.Replace(".json", i + ".sh")));
                while (all.Count > 0)
                    foreach (var item in sw)
                    {
                        if (all.Count == 0)
                            break;
                        var tmp = all.First();
                        all.Remove(tmp);
                        item.WriteLine(tmp);
                        item.Flush();
                    }
                foreach (var item in sw)
                    item.Close();

            }
            catch (Exception) { }
        }
    }
}
