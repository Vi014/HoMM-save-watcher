using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading;

namespace saveWatcher
{
    class saveWatcher
    {
        private static readonly object locker = new object();
        private static DateTime lastRead = DateTime.MinValue;
        private static String filePath = @"./", fileName = "AUTOSAVE.CGM", fileType = "CGM";
        private static Int32 month = 1, week = 1, day = 1;

        static void Main(String[] args)
        {
            try
            {
                String[] cfg = File.ReadAllLines("config.txt");
                filePath = cfg[0];
                fileName = cfg[1];
                fileType = cfg[2];
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                filePath = @"./";
                fileName = "AUTOSAVE.CGM";
                fileType = "CGM";
            }

            if(args.Length == 3)
            {
                try
                {
                    month = Convert.ToInt32(args[0]);
                    week  = Convert.ToInt32(args[1]);
                    day   = Convert.ToInt32(args[2]);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine(ex.Message);
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

            System.Console.WriteLine($"Ready to monitor save file {filePath}{fileName}, starting with turn {month}{week}{day}. \r\nPress any key to abort.");
            System.Console.ReadLine();
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            lock (locker)
            {
                DateTime now = DateTime.Now;

                if (now.Subtract(lastRead).TotalMilliseconds > 1000)
                {
                    lastRead = now;
                    
                    String date = $"{month}{week}{day}.{fileType}";
                    System.Console.WriteLine(date);

                    try
                    {
                        Thread.Sleep(500);
                        File.Copy(filePath+fileName, filePath+date+"."+fileType, true);
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine(ex.Message);
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
