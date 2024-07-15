using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading;

namespace saveWatcher
{
    /* 
        TODO: 
        - implement case insensitive checks
        - simplify initializations in ChangeConfig()
        - try-catch for opening save file and its directory
        - main operating block refactor
        - remove unnecessary using blocks
        - rename project
    */
    class SaveWatcher
    {
        private static readonly object locker = new object();
        private static DateTime lastRead = DateTime.MinValue;
        private static String filePath = "./", fileName = "AUTOSAVE.CGM", fileType = "CGM";
        private static Int32 timeout = 2000;
        private static Int32 month = 1, week = 1, day = 1;

        static void Main(String[] args)
        {
            if(File.Exists("config.txt")) 
            {
                Boolean readSuccess = false;
                while(!readSuccess)
                {
                    try
                    {
                        String[] cfg = File.ReadAllLines("config.txt");

                        if(cfg.Length >= 4)
                        {    
                            filePath = cfg[0];
                            fileName = cfg[1];
                            fileType = cfg[2];
                            if(!Int32.TryParse(cfg[3], out timeout)) break;
                        }
                        else break;

                        readSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        Console.Write("Error while opening config file! If the issue persists, check if the program has permission to read and write in the folder it's installed in. Try again? [Y/n/i]: ");
                        char c = (Console.ReadLine() ?? "Y").Trim()[0];
                        if(c == 'n') 
                        {
                            readSuccess = true;
                            ChangeConfig();
                        }
                        else if (c == 'i') Console.WriteLine(ex.Data);
                    }
                }
                if(!readSuccess) 
                {
                    Console.WriteLine("Unable to load configuration as the config file is improperly formatted.");
                    ChangeConfig();
                }
            }
            else
            {
                Console.WriteLine("No config file found.");
                ChangeConfig();
            }

            if(args.Length >= 3)
            {
                try
                {
                    month = Convert.ToInt32(args[0]);
                    week  = Math.Min(Convert.ToInt32(args[1]), 4);
                    day   = Math.Min(Convert.ToInt32(args[2]), 7);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while reading start date passed via arguments: {ex.Message} \r\nDefaulting to 111.");
                    month = week = day = 1;
                }
            }
            else month = week = day = 1;

            using var watcher = new FileSystemWatcher(filePath);

            watcher.Filter = fileName;
            watcher.NotifyFilter = NotifyFilters.LastWrite;

            watcher.Changed += OnChanged;
            watcher.Created += OnChanged;

            watcher.EnableRaisingEvents = true;  

            Console.WriteLine($"Ready to monitor save file {filePath}{fileName}, starting with turn {month}{week}{day}. \r\nPress any key to abort.");
            Console.ReadLine();
        }

        private static void ChangeConfig()
        {
            Console.Write("Load default settings? [y/N]: ");
            String[] cfg = ["./AUTOSAVE.CGM", "2000"];

            if((Console.ReadLine() ?? "N").Trim() != "y")
            {
                Console.Write("Full path to monitored file: (default ./AUTOSAVE.CGM): ");
                cfg[0] = Console.ReadLine() ?? "";

                // extracting the directory path, filename, and extension from the full path
                Int32 splitPoint = cfg[0].LastIndexOfAny(new char[] {'\\', '/'});
                filePath = cfg[0].Substring(0, splitPoint + 1); // +1 to the length here to include the final slash in the directory path
                fileName = cfg[0].Substring(splitPoint + 1); // +1 to the length here to *not* have the slash be part of the filename
                String[] extension = fileName.Split('.');
                fileType = extension.Length == 2 ? extension[1] : ""; // we do this in case the user wants to monitor a file with no extension 

                while(String.IsNullOrWhiteSpace(cfg[0]) || splitPoint == -1) // LastIndexOfAny returns -1 if no instances of the listed characters are found (in other words, if the user only entered a filename and no path to it)
                {
                    Console.Write("Invalid value, please try again: ");
                    cfg[0] = Console.ReadLine() ?? "";

                    splitPoint = cfg[0].LastIndexOfAny(new char[] {'\\', '/'});
                    filePath = cfg[0].Substring(0, splitPoint + 1);
                    fileName = cfg[0].Substring(splitPoint + 1);
                    extension = fileName.Split('.');
                    fileType = extension.Length == 2 ? extension[1] : "";
                }

                Console.Write("Timeout between creating new files: (in ms, used for making sure the same day doesn't get saved multiple times, ideal value is slightly higher than the maximum duration of AI turns, default is 2000): ");
                cfg[1] = (Console.ReadLine() ?? "").Trim();

                while(!Int32.TryParse(cfg[1], out timeout))
                {
                    Console.Write("Invalid value, please try again: ");
                    cfg[1] = (Console.ReadLine() ?? "").Trim();
                }
            }
            else
            {
                filePath = "./";
                fileName = "AUTOSAVE.CGM";
                fileType = "CGM";
                timeout = 2000;
            }

            Console.Write("Save settings to config file? [Y/n]: ");
            if((Console.ReadLine() ?? "Y").Trim() != "n")
            {
                Boolean saveSuccess = false;
                while(!saveSuccess)
                {
                    try
                    {
                        File.WriteAllLines("config.txt", new String[] {filePath, fileName, fileType, timeout.ToString()});
                        saveSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        Console.Write("Error while writing to file! Try again? [Y/n/i]: ");
                        String asdf = (Console.ReadLine() ?? "Y").Trim();
                        System.Console.WriteLine(asdf);
                        char c = (Console.ReadLine() ?? "Y").Trim()[0];
                        if(c == 'n') saveSuccess = true;
                        else if (c == 'i') Console.WriteLine(ex.Data);
                    }
                }
            }
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            lock (locker)
            {
                DateTime now = DateTime.Now;

                if (now.Subtract(lastRead).TotalMilliseconds > timeout)
                {
                    lastRead = now;
                    
                    String newSaveName = $"{month}{week}{day}.{fileType}";
                    Console.WriteLine(newSaveName);

                    try
                    {
                        Thread.Sleep(500);
                        File.Copy(filePath+fileName, filePath+newSaveName, true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    day++;
                    if(day > 7) 
                    {
                        day = 1;
                        week++;
                        if(week > 4)
                        {
                            week = 1;
                            month++;
                        }
                    }
                }
            }
        }
    }
}
