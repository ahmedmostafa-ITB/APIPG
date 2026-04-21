using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;

namespace SelfServiceAPI.Classes
{
    public class LoggingManager
    {
        static object obj = new object();
        public static void LogException(string message, string stackTrace, DateTime dateTime, string callStack, string sender)
        {
            CreateExceptionFile(callStack, sender, message, stackTrace, dateTime);
        }
        private static void CreateExceptionFile(string errorpackage, string errortask, string errortaskinformation, string errormessage, DateTime errordate)
        {
            string folderPath = ConfigurationManager.AppSettings["ErrorLogs"];
            //string filePath = folderPath + "Errors.csv";
            string fileName;
            string activeFilePath = folderPath + "ActiveFile.txt";
            string header = "Error Package,Error Task,Error Task Information,Error Message,Error Date" + Environment.NewLine;
            string content = errorpackage + "|" + errortask + "|" + errortaskinformation + "|" + errormessage + "|" + errordate;
            content = content.Replace(',', '-');
            content = content.Replace("\n", " ");
            content = content.Replace("\r", " ");
            content = content.Replace('|', ',') + Environment.NewLine;
            lock (obj)
            {
                try
                {
                    if (!File.Exists(activeFilePath))
                    {
                        string activeFileName = folderPath + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day + " - " + DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second + ".csv";
                        File.WriteAllText(activeFilePath, activeFileName);

                        File.WriteAllText(activeFileName, header);
                        File.AppendAllText(activeFileName, content);
                    }
                    else
                    {
                        using (StreamReader sr = new StreamReader(activeFilePath))
                        {
                            fileName = sr.ReadToEnd();
                        }
                        if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
                        {
                            var fi = new FileInfo(fileName);
                            long size = fi.Length / 1048576;
                            if (size >= int.Parse(ConfigurationManager.AppSettings["MaximumSizeErrorLogMB"]))
                            {
                                fileName = folderPath + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day + " - " + DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second + ".csv";
                                File.WriteAllText(fileName, header);
                                File.AppendAllText(fileName, content);
                                File.WriteAllText(activeFilePath, fileName);
                            }
                            else
                            {
                                File.AppendAllText(fileName, content);
                            }

                        }
                        else
                        {
                            fileName = folderPath + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day + " - " + DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second + ".csv";
                            File.WriteAllText(fileName, header);
                            File.AppendAllText(fileName, content);
                            File.WriteAllText(activeFilePath, fileName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    fileName = folderPath + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day + " - " + DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second + ".csv";
                    File.WriteAllText(fileName, header);
                    File.AppendAllText(fileName, content);
                    File.WriteAllText(activeFilePath, fileName);
                }
            }
        }

    }
}