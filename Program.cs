using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace DataCenter
{
    internal class Program
    {
        private const string folder = "../../Modules/";
        private static readonly Mutex moduleMutex = new Mutex();
        private static readonly List<Module> modules = new List<Module>();
        private static readonly Dictionary<string, IDisposable> modulesLoading = new Dictionary<string, IDisposable>();

        public static void Main(string[] args)
        {
#if DEBUG
			FileCreated(null, new FileSystemEventArgs(WatcherChangeTypes.Created, "../../Modules/FoodWarsChecker", Path.GetFileName("FoodWarsChecker")));
#else
			FileSystemWatcher watcher = new FileSystemWatcher(folder);
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Changed += FileChanged;
            watcher.Created += FileCreated;
            watcher.Deleted += FileDeleted;
            watcher.Renamed += FileRenamed;
            watcher.EnableRaisingEvents = true;

            foreach (string dir in Directory.GetDirectories(folder))
            {
                string directory = Path.GetFullPath(dir);
                FileCreated(null, new FileSystemEventArgs(WatcherChangeTypes.Created, directory, Path.GetFileName(dir)));
            }
#endif

            while (Console.ReadKey().Key != ConsoleKey.Escape)
            {
            }

            foreach (Module module in modules)
            {
                module.Dispose();
            }
        }

        private static void LoadModule(string dir)
        {
            // throttle file changes
            // windows likes to fire multiple folder changes for 1 file being changed

            string baseDir = Path.GetFullPath(folder);
            string name = Path.GetFullPath(dir).Substring(baseDir.Length);
            if (name.IndexOfAny(new[] { '/', '\\' }) >= 0)
            {
                name = name.Substring(0, name.IndexOfAny(new[] { '/', '\\' }));
            }

            if (modulesLoading.ContainsKey(name))
            {
                modulesLoading[name].Dispose();
                modulesLoading.Remove(name);
            }

            IDisposable handle = Utils.SetTimeout(() =>
            {
                moduleMutex.WaitOne();
                Module module = modules.FirstOrDefault(m => m.Name == name);
                if (module != null)
                {
                    module.Interrupt();
                    Console.WriteLine("Reloading {0}", name);
                }
                else
                {
                    module = new Module(name, folder + name);
                    modules.Add(module);
                    Console.WriteLine("Loading {0}", name);
                }
                moduleMutex.ReleaseMutex();
                module.Start();
            }, 1000);

            modulesLoading.Add(name, handle);
        }

        private static void FileCreated(object sender, FileSystemEventArgs e)
        {
            LoadModule(e.FullPath);
        }

        private static void FileChanged(object sender, FileSystemEventArgs e)
        {
            LoadModule(e.FullPath);
        }

        private static void FileDeleted(object sender, FileSystemEventArgs e)
        {
            LoadModule(e.FullPath);
        }

        private static void FileRenamed(object sender, RenamedEventArgs e)
        {
            LoadModule(e.FullPath);
        }
    }
}
