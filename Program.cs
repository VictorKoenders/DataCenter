using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DataCenter.Web;

//#define DEBUG_ONE_MODULE

namespace DataCenter
{
    internal static class Program
    {
        private const string Folder = "../../Modules/";
        private static readonly Mutex ModuleMutex = new Mutex();
        private static readonly List<Module> Modules = new List<Module>();
        private static readonly Dictionary<string, IDisposable> ModulesLoading = new Dictionary<string, IDisposable>();

        public static void Main()
        {
#if DEBUG && DEBUG_ONE_MODULE
            FileCreated(null, new FileSystemEventArgs(WatcherChangeTypes.Created, Path.GetFullPath("..\\..\\Modules\\LinkedInConnector"), "LinkedInConnector"));
#else
            FileSystemWatcher watcher = new FileSystemWatcher(Folder)
	        {
		        IncludeSubdirectories = true,
		        NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
	        };

	        watcher.Changed += FileChanged;
            watcher.Created += FileCreated;
            watcher.Deleted += FileDeleted;
            watcher.Renamed += FileRenamed;

            watcher.EnableRaisingEvents = true;

            foreach (string dir in Directory.GetDirectories(Folder))
            {
                string directory = Path.GetFullPath(dir);
                FileCreated(null, new FileSystemEventArgs(WatcherChangeTypes.Created, directory, Path.GetFileName(dir)));
            }
#endif

            ApiManager.Instance.Start();

            while (Console.ReadKey().Key != ConsoleKey.Escape)
            {
			}

			ApiManager.Instance.Stop();

			foreach (Module module in Modules)
            {
                module.Dispose();
            }
        }

        private static void LoadModule(string dir)
        {
            // throttle file changes
            // windows likes to fire multiple Folder changes for 1 file being changed

            string baseDir = Path.GetFullPath(Folder);
            string name = Path.GetFullPath(dir).Substring(baseDir.Length);
            if (name.IndexOfAny(new[] { '/', '\\' }) >= 0)
            {
                name = name.Substring(0, name.IndexOfAny(new[] { '/', '\\' }));
            }

            if (ModulesLoading.ContainsKey(name))
            {
                ModulesLoading[name].Dispose();
                ModulesLoading.Remove(name);
            }

            IDisposable handle = Utils.SetTimeout(() =>
            {
                ModuleMutex.WaitOne();
                Module module = Modules.FirstOrDefault(m => m.Name == name);
				ModuleMutex.ReleaseMutex();
				if (module != null)
				{
					module.Console.log("Reloading", name);
					module.Interrupt();
                }
                else
                {
                    module = new Module(name, Folder + name);
					ModuleMutex.WaitOne();
					Modules.Add(module);
					ModuleMutex.ReleaseMutex();
					module.Console.log("Loading", name);
                }

	            try
	            {
		            module.Start();
	            }
	            catch(Exception ex)
	            {
					ModuleMutex.WaitOne();
					module.Console.log("Could not load", name, ":", ex.Message);

					Modules.Remove(module);
					ModuleMutex.ReleaseMutex();
					

					module.Dispose();
				}
            }, 1000);

            ModulesLoading.Add(name, handle);
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

        private static void FileRenamed(object sender, FileSystemEventArgs e)
        {
            LoadModule(e.FullPath);
        }
    }
}
