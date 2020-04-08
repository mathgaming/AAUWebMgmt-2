﻿using ITSWebMgmt.Caches;
using ITSWebMgmt.Connectors;
using ITSWebMgmt.Models;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;

namespace ITSWebMgmt.Helpers
{
    public class ComputerListModel
    {
        private static string path = Path.Combine(Directory.GetCurrentDirectory(), "computer-list/");
        private static readonly string filename = path + "computer-list.txt";
        private static readonly string emailFilename = path + "computer-list-emails.txt";
        private static List<string> emails = new List<string>();
        private static readonly string mailbody = "Computer list is attached.\n";
        public static bool Running { private set; get; } = false;

        public ComputerListModel()
        {
            if (!Running)
            {
                Running = true;
                MakeList();
                CombineLists();
                string stats = GetStats();
                foreach (var e in emails)
                {
                    EmailHelper.SendEmailWithAttachment("Computer list", mailbody + stats, e, path + "computer-list-full.txt");
                }
                CleanUp();
                emails.Clear();
                Running = false;
            }
        }

        public string GetStats()
        {
            int staff = 0;
            int onedrive = 0;
            int windows = 0;
            int mac = 0;
            int both = 0;
            int missingOnedrive = 0;
            int errorCount = 0;
            int failedCount = 0;
            int missingComputer = 0;
            bool hasMac = false;
            bool hasWindows = false;
            bool haveComputerWithOnedrive = false;
            bool userHaveOnedrive = false;
            string line;
            string prevPerson = "";

            StreamReader file = new StreamReader(path + "computer-list-full.txt");
            file.ReadLine();
            while (true)
            {
                line = file.ReadLine();
                string person;
                if (line != null)
                {
                    if (line.Contains("Error finding"))
                    {
                        errorCount++;
                    }
                    if (line.Contains("Failed to get data for"))
                    {
                        failedCount++;
                    }
                    if (line.Contains("No computer found for user"))
                    {
                        missingComputer++;
                    }
                    string[] parts = line.Split(";");
                    person = parts[0];

                    string os = parts[5];
                    if (os == "mac")
                    {
                        hasMac = true;
                    }
                    else if (os == "windows")
                    {
                        hasWindows = true;
                    }
                    string computerOnedrive = parts[6];
                    if (computerOnedrive == "True")
                    {
                        haveComputerWithOnedrive = true;
                    }

                    string usesOnedrive = parts[2];
                    if (usesOnedrive == "True")
                    {
                        userHaveOnedrive = true;
                    }
                }
                else
                {
                    person = "";
                }

                if (person != prevPerson)
                {
                    if (userHaveOnedrive)
                    {
                        onedrive++;
                    }
                    if (haveComputerWithOnedrive && !userHaveOnedrive)
                    {
                        missingOnedrive++;
                    }
                    if (hasMac && hasWindows)
                    {
                        both++;
                    }
                    if (hasWindows)
                    {
                        windows++;
                    }
                    if (hasMac)
                    {
                        mac++;
                    }
                    staff++;
                    hasWindows = false;
                    hasMac = false;
                    userHaveOnedrive = false;
                    haveComputerWithOnedrive = false;
                }

                prevPerson = person;
                if (line == null)
                {
                    break;
                }
            }

            file.Close();

            return $"Unikke brugernavne: {staff}\n" +
            $"På OneDrive: {onedrive}\n" +
            $"Ikke på OneDrive: {staff - onedrive}\n" +
            $"\n" +
            $"Fejl:" +
            $"Fejlede for brugere: {errorCount}\n" +
            $"Kunne ikke finde informaion om computer: {failedCount}" +
            $"\n" +
            $"Platforme:\n" +
            $"Brugere med Windows: {windows}\n" +
            $"Brugere med MAC: {mac}\n" +
            $"Brugere med begge: {both}\n" +
            $"Brugere uden computer: {missingComputer}\n" +
            $"Brugere på OneDrive men med Windows-computer ikke på OneDrive: {missingOnedrive}\n";
        }

        public static void ContinueIfStopped()
        {
            if (File.Exists(emailFilename))
            {
                int batchId = 0;
                while (true)
                {
                    string batchFilename = $"{path}computer-list-{batchId}.txt";
                    if (!File.Exists(batchFilename))
                    {
                        break;
                    }
                    batchId++;
                }
                File.Delete($"{path}computer-list-{batchId - 1}.txt");

                StreamReader file = new StreamReader(emailFilename);
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    emails.Add(line);
                }
                file.Close();

                new ComputerListModel();
            }
        }

        public static void AddEmail(string email)
        {
            if (!emails.Contains(email))
            {
                emails.Add(email);
                string outFilename = emailFilename;
                using StreamWriter outFile = File.AppendText(outFilename);
                outFile.WriteLine(email);
                outFile.Close();
            }
        }

        public void CleanUp()
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            DirectoryInfo di = new DirectoryInfo(path);

            foreach (FileInfo file in di.GetFiles())
            {
                if (file.Name != "computer-list-full.txt")
                {
                    file.Delete();
                }
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        public void CombineLists()
        {
            int batchId = 0;
            List<List<string>> batches = Readbatches();
            string outFilename = path + "computer-list-full.txt";
            using StreamWriter outFile = new StreamWriter(outFilename);
            outFile.WriteLine("upn;last logon for user;user uses onedrive;staff;computername;os;uses onedrive;free disk space (GB);virtual?;last login date;last login user");

            foreach (var batch in batches)
            {
                string batchFilename = $"{path}computer-list-{batchId}.txt";
                if (File.Exists(batchFilename))
                {
                    StreamReader file = new StreamReader(batchFilename);
                    string line;
                    while ((line = file.ReadLine()) != null)
                    {
                        outFile.WriteLine(line);
                    }
                    file.Close();
                }
                batchId++;
            }

            outFile.Close();
        }

        private List<List<string>> Readbatches()
        {
            List<List<string>> batches = new List<List<string>>();
            StreamReader file = new StreamReader(filename);
            string line;
            while ((line = file.ReadLine()) != null)
            {
                batches.Add(line.Split(";").ToList());
            }
            file.Close();

            return batches;
        }

        public void MakeList()
        {
            List<List<string>> batches = new List<List<string>>();

            if (File.Exists(filename))
            {
                batches = Readbatches();
            }
            else
            {
                DirectoryEntry de = null;
                foreach (var item in DirectoryEntryCreator.CreateNewDirectoryEntry("GC:").Children)
                {
                    de = (DirectoryEntry)item;
                }
                DirectorySearcher userSearcher = new DirectorySearcher(de);
                userSearcher.SearchScope = SearchScope.Subtree;
                userSearcher.PageSize = 10000;
                userSearcher.Filter = "(&(objectClass=user)(aauUserClassification=employee))";

                var res = userSearcher.FindAll();
                List<string> allMembers = new List<string>();

                foreach (SearchResult user in res)
                {
                    allMembers.Add(user.Path);
                }

                var membersADPath = allMembers.Distinct().ToList().Where(d => !d.Contains("DC=iiqdev") && !d.Contains("DC=aau-it") && !d.Contains("DC=admt") && !d.Contains("CN=kursus")).ToList();

                int count = 0;
                List<string> batch = new List<string>();

                using StreamWriter file = new StreamWriter(filename);
                foreach (var adpath in membersADPath)
                {
                    if (count == 100)
                    {
                        file.WriteLine(string.Join(";", batch));
                        batches.Add(batch);
                        batch = new List<string>();
                        count = 0;
                    }
                    batch.Add(adpath);
                    count++;
                }

                file.WriteLine(string.Join(";", batch));
                batches.Add(batch);
                file.Close();
            }

            int batchNumber = 0;

            foreach (var b in batches)
            {
                RunBatch(b, batchNumber);
                batchNumber++;
            }
        }

        private List<string> getWindowsInformation(UserModel model, string formattedName)
        {
            List<string> computerInfo = new List<string>();

            foreach (ManagementObject o in model.getUserMachineRelationshipFromUserName(formattedName))
            {
                var computerName = o.Properties["ResourceName"].Value.ToString();
                var computerModel = new WindowsComputerModel(computerName);
                var onedrive = "Unknown";
                try
                {
                    onedrive = OneDriveHelper.ComputerUsesOneDrive(computerModel.ADcache).ToString();
                }
                catch (Exception)
                {
                }
                
                var @virtual = "Unknown";
                var computerNameUC = computerName.ToUpper();
                if (computerNameUC.StartsWith("AAU"))
                {
                    @virtual = "False";
                }
                else if (computerNameUC.StartsWith("SRV"))
                {
                    @virtual = "True";
                }
                try
                {
                    var diskspace = -1;
                    if (SCCM.HasValues(computerModel.Disk))
                    {
                        var disk = computerModel.LogicalDisk.OfType<ManagementObject>().FirstOrDefault();
                        diskspace = disk.Properties["FreeSpace"].Value != null ? (int.Parse(disk.Properties["FreeSpace"].Value.ToString()) / 1024) : -1;
                    }
                    string time = computerModel.System.GetProperty("LastLogonTimestamp");
                    var date = time != null ? DateTimeConverter.Convert(time) : "";
                    var lastLoginUser = computerModel.System.GetProperty("LastLogonUserDomain") + "\\\\" + computerModel.System.GetProperty("LastLogonUserName");
                    computerInfo.Add($"{computerName};windows;{onedrive};{diskspace};{@virtual};{date};{lastLoginUser}");
                }
                catch (Exception e)
                {
                    computerInfo.Add($"{computerName};windows;{onedrive};;{@virtual};;;Failed to get data for {computerName}");
                }
            }

            return computerInfo;
        }

        private List<string> getMacInformation(string upn)
        {
            List<string> computerInfo = new List<string>();
            JamfConnector jamf = new JamfConnector();
            foreach (var email in new UserModel(upn, false).getUserMails())
            {
                foreach (var computerName in jamf.getComputerNamesForUser(email))
                {
                    var @virtual = "Unknown";
                    var onedrive = "";
                    var date = "";
                    var lastLoginUser = "";
                    MacComputerModel macComputer = new MacComputerModel(computerName);
                    var diskspace = macComputer.FreeSpace;
                    if (computerName.StartsWith("AAU"))
                    {
                        @virtual = "False";
                    }
                    if (computerName.StartsWith("AAUVM"))
                    {
                        @virtual = "True";
                    }
                    computerInfo.Add($"{computerName};mac;{onedrive};{diskspace};{@virtual};{date};{lastLoginUser}");
                }
            }

            return computerInfo;
        }

        public List<string> lookupUser(string adpath)
        {
            List<string> lines = new List<string>();
            string upn = "";
            try
            {
                adpath = adpath.Replace("GC://aau.dk/", "");
                var split = adpath.Split(',');
                var name = split[0].Replace("CN=", "");
                var domain = split.Where(s => s.StartsWith("DC=")).ToArray()[0].Replace("DC=", "");
                upn = $"{name}@{domain}.aau.dk";
                adpath = "LDAP://" + adpath;

                UserModel model = new UserModel(adpath, "");
                if (!model.IsDisabled)
                {
                    string formattedName = string.Format("{0}\\\\{1}", domain, name);
                    List<string> computerInfo = new List<string>();

                    computerInfo.AddRange(getWindowsInformation(model, formattedName));
                    computerInfo.AddRange(getMacInformation(upn));

                    string staff = "Other";
                    if (adpath.Contains("Staff"))
                    {
                        staff = "Staff";
                    }
                    else if (adpath.Contains("Guests"))
                    {
                        staff = "Guests";
                    }
                    var lastLogon = model.LastLogon;
                    var usesOnedrive = OneDriveHelper.doesUserHaveDeniedFolderRedirect(model);
                    foreach (var computer in computerInfo)
                    {
                        lines.Add($"{upn};{lastLogon};{usesOnedrive};{staff};{computer}");
                    }

                    if (computerInfo.Count == 0)
                    {
                        lines.Add($"{upn};{lastLogon};{usesOnedrive};{staff};;;;;;;;No computer found for user");
                    }
                }
            }
            catch (Exception e)
            {
                lines.Add($"{upn};;;;;;;;;;;Error finding {adpath}");
                Console.WriteLine(e.Message);
            }

            return lines;
        }

        public void RunBatch(List<string> adpaths, int batch)
        {
            string batchFilename = $"{path}computer-list-{batch}.txt";
            if (!File.Exists(batchFilename))
            {
                using StreamWriter file = new StreamWriter(batchFilename);
                foreach (var adpath in adpaths)
                {
                    List<string> lines = lookupUser(adpath);

                    foreach (var line in lines)
                    {
                        file.WriteLine(line);
                    }

                    Thread.Sleep(1000);
                }

                file.Close();
            }
        }
    }
}
