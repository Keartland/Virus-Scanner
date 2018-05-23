using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace ScannerV2
{
    class Program
    {
        static void Main(string[] args)
        {
            checkMaster(); // checks if the master file exists
            if (args.Length == 0) // scan all running files
            {
                Process[] processes = Process.GetProcesses(); // gets processes
                List<string> procs = new List<string>(); // sets up list
                foreach (Process process in processes) // iterates through all the processes
                {
                    try { procs.Add(process.MainModule.FileName); } // adds the file locations to a new list
                    catch { } // does nothing if it catches an error as that will be a file that is not accessible
                }

                HashSet<string> procsSet = new HashSet<string>(procs); // removes repeats
                String[] FileList = new String[procsSet.Count]; // sets up a string array with the same lenght as the hashset
                procsSet.CopyTo(FileList);// puts the HashSet back into a string array
                scanFile(FileList);
            }
            else if (args[0] == "-a")
            {
                List<string> files = DirSearch("C:\\");
                String[] allFiles = new String[files.Count];
                Console.Clear();
                scanFile(allFiles);
            }
            else //scanning attached file 
            {
                scanFile(args);
            }
        }
        private static void scanFile(string[] files)
        {
            double estTM = 0;
            Stopwatch myStopWatch = new Stopwatch(); // initialises the stopwatch
            foreach (string file in files)
            {
                if (File.Exists(file))
                {
                    Console.WriteLine(Array.IndexOf(files, file) + "\\" + files.Length + " Est time remaining: {0} s", estTM); // prints the current file out of all the files
                    myStopWatch.Reset(); // resets the stop watch to 0
                    myStopWatch.Start(); // starts the stop watch
                    Directory.CreateDirectory("Temp"); // create a directory called temp
                    string cpy = "Temp\\" + file.Split('\\').Last(); // sets up the temp location
                    File.Copy(file, cpy, true); // copys the file to the temp location
                    string fileHash = GetMD5HashFromFile(cpy); // generates the hash for the task
                    Console.WriteLine("\nStarted Scanning: {0} ({1})", file.Split('\\').Last(), fileHash); // warns the user the scan has started
                    bool inFile = File.ReadLines("master.md5").Contains(fileHash); // checks the master list for the hash
                    Console.Clear(); // clears console
                    if (inFile) // if the hash is in the master file
                    {
                        while (true) // start loop
                        {
                            Console.Clear(); // clears console
                            Console.WriteLine("{0} is infected", file.Split('\\').Last()); // tells the user whether it is clean or not
                            Console.WriteLine("Would you like to delete this file (y or n)? "); // asks the user if they want to delete the suspected virus
                            string ans = Console.ReadLine(); // collects the users answer
                            if (ans != "n" && ans != "y") // checks they've entered a vaild answer
                            {
                                Console.WriteLine("Please enter y or n..."); // tells them if they do it wrong
                            }
                            else
                            {
                                if (ans == "n") { break; } // doesnt delete the file if it is no and breaks out the loop
                                else if (ans == "y") // deletes the file if yes
                                {
                                    foreach (var process in Process.GetProcessesByName(file.Split('\\').Last().Split('.').First())) // goes through all the processes with the name of the file and kills it.
                                    {
                                        process.Kill(); // kills the process
                                    }
                                    try
                                    {
                                        File.Delete(cpy); // deletes the tmp one
                                        File.Delete(file); // deletes the actuall file
                                    }
                                    catch { }
                                    break; // breaks out the loop
                                }
                            }
                        }
                    }
                    else { Console.WriteLine("{0} is clean", file.Split('\\').Last()); } // tells the user whether it is clean or not
                    myStopWatch.Stop(); // stops the stop watch
                    estTM = (files.Length - Array.IndexOf(files, file)) * myStopWatch.ElapsedMilliseconds / 1000; // works out the estimated time remaining
                    File.Delete(cpy); // deletes the temp files
                    Directory.Delete("Temp");
                }
                else
                {
                    Console.WriteLine("File {0} Doesn't exist... cannot scan",file);
                }
            }
        }
        private static string GetMD5HashFromFile(string fileName) // generates the md5 hash
        {
            FileStream file = new FileStream(fileName, FileMode.Open); // opens thhe file to read the data
            MD5 md5 = new MD5CryptoServiceProvider(); // sets up a new MD5CryptoServiceProvider
            byte[] retVal = md5.ComputeHash(file); // creates a byte array 
            file.Close(); // closes the file to stop memeory leak
            StringBuilder sb = new StringBuilder(); // creates a StringBuilder
            for (int i = 0; i < retVal.Length; i++) // itterates through each thing in the the byte array
            {
                sb.Append(retVal[i].ToString("x2")); // converts the bytes to a string and adds it the the StringBuilder
            }
            return sb.ToString(); // returns the string version
        }
        private static void checkMaster()
        {
            if (File.Exists("master.md5")) { Console.WriteLine("Ready To Scan"); } // checks for the master hash file and tells the user it can scan if it exists
            else
            {
                Console.WriteLine("No hash file found... \nDownloading now please wait..."); // tells the user the file isnt there and warns them that it will download
                Directory.CreateDirectory("hashFiles"); // creates the directory to download the hash files to
                for (int i = 0; i <= 300; i++) // loop for the files
                {
                    string url = "https://virusshare.com/hashes/VirusShare_" + i.ToString("D5") + ".md5"; // sets up the link and sets i to be 5 digits in form 0000x
                    using (var client = new WebClient()) // opens a wbe client
                    {
                        client.DownloadFile(url, "hashFiles\\" + i.ToString("D5") + ".md5"); // downloads the file
                    }
                    Console.WriteLine("Downloaded " + url); // tells the user it has downloaded it
                }
                Console.WriteLine("Finsished Downloading...\nConcatonating files..."); // tells the user it is going to combine the files
                File.WriteAllLines("master.md5",
                    Directory.GetFiles("hashFiles", "*.md5").SelectMany(f => File.ReadLines(f).Concat(new[] { Environment.NewLine }))); // combines the files
                Console.WriteLine("FINISHED!"); // tells the user it has finished
                string[] filePaths = Directory.GetFiles("hashFiles");
                foreach (string filePath in filePaths) { File.Delete(filePath); }
                System.IO.Directory.Delete("hashFiles"); // deletes the folder
                Console.Clear();  // clears the cmd window
            }
        }
        private static List<string> DirSearch(string sDir)
        {
            List<string> files = new List<string>();
            try
            {
                foreach (string f in Directory.GetFiles(sDir))
                {
                    if (f.Substring(f.Length - 4) == ".exe")
                    {
                        Console.WriteLine(f);
                        files.Add(f);
                    }
                }
                foreach (string d in Directory.GetDirectories(sDir)) { files.AddRange(DirSearch(d)); }
            }
            catch (System.Exception excpt) { Console.WriteLine(excpt.Message); }
            return files;
        }
    }
}