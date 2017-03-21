using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft;
using System.Diagnostics;

namespace DSVImportFile
{
    class Program
    {

        [STAThread]
        static void Main(string[] args)
        {

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(ExceptionHelper.LogWebException);

            Application.SetUnhandledExceptionMode(
              UnhandledExceptionMode.CatchException);

           BusinessFunctions businessFunctions = new BusinessFunctions();
            DirectoryInfo diTemp = null;


            Debug.WriteLine("Getting Inbound Directory files.");
            Console.WriteLine();
            Console.WriteLine("Getting Inbound Directory files.");
            //Poonam patel, Date: 03/09/2017,Desc: Getting Inbound Directory to convert file from Inbound
            DirectoryInfo InboundDirectory = new DirectoryInfo(ConfigHelper.GetConfigSetting("InputFile"));
            List<string> InboundDirectoryFiles = businessFunctions.GetDirectoryFiles(InboundDirectory, DirectoryFileType.InboundNormalFiles,"");
            List<string> InboundDirectoryInprogressFiles = businessFunctions.GetDirectoryFiles(InboundDirectory, DirectoryFileType.InboundFileWithPrefix, ".inprogress");
          
            #region If there is any Inprogress file
            Debug.WriteLine("Inbound files contain inprogress file then get count.");
            
            //Poonam patel, Date: 03/09/2017,Desc: Checking in Directory if its containing any inprogress files or not if it's containing then it will processed first
            if (InboundDirectoryInprogressFiles.ToList().Count > 0)
            {
                FileInfo Inboundfile = new FileInfo(InboundDirectoryInprogressFiles.FirstOrDefault());
                int ErrorOccurenceCount = businessFunctions.GetErrorCount(Inboundfile.Name);
                string fileprefix = businessFunctions.GetFilenamePrefix(Inboundfile);

                #region If it fails 3 times to convert file into pdf
               
                if (ErrorOccurenceCount >= 3)
                {
                  
                    //Poonam patel, Date: 03/09/2017,Desc: Checking for Folder with given filePreFix is exist in Temp folder or not
                    if (Directory.Exists(ConfigHelper.GetConfigSetting("TempFile") + fileprefix))
                    {
                        
                        string[] TempPreFixFiles = Directory.GetFiles(ConfigHelper.GetConfigSetting("TempFile") + fileprefix);

                        //looping all files from Fileprefix folder of Temp folder to move it all into ProcessedError folder
                        foreach (string TempPreFixFile in TempPreFixFiles)
                        {
                            FileInfo Processedfile = new FileInfo(TempPreFixFile);
                            File.Move(TempPreFixFile, ConfigHelper.GetConfigSetting("ProcessErrorFile") + Processedfile.Name);
                        }
                        Debug.WriteLine("Moved " + Inboundfile.Name + " to ProcessError folder");
                        Console.WriteLine("Moved " + Inboundfile.Name + " to ProcessError folder");
                        //Getting all files from That folder which have given FilePreFix ,to delete it from Inbound folder 
                        List<string> InputFilesToProcess = businessFunctions.GetDirectoryFiles(InboundDirectory, DirectoryFileType.InboundFileWithPrefix, fileprefix);
                       
                        foreach (string item in InputFilesToProcess)
                        {
                            FileInfo Processedfile = new FileInfo(item);
                            File.Delete(item);
                        }
                        Debug.WriteLine("deleted" + Inboundfile.Name + " to Inbound folder");
                        Console.WriteLine("deleted" + Inboundfile.Name + " to Inbound folder");
                    }
                }
                #endregion

                #region If there is less then 3 but 1 or more then 1 file exist
                else
                {
                    string[] tempFiles;
                   if (!Directory.Exists(ConfigHelper.GetConfigSetting("TempFile") + fileprefix))
                    {
                       diTemp = businessFunctions.CreateDirectorywithfolder("TempFile", fileprefix, FolderType.folderwithprefix);
                   }
                   diTemp = new System.IO.DirectoryInfo(ConfigHelper.GetConfigSetting("TempFile") + fileprefix);

                    tempFiles = Directory.GetFiles(ConfigHelper.GetConfigSetting("TempFile") + fileprefix);

                    foreach (string item in tempFiles)
                    {
                        if (Directory.Exists(ConfigHelper.GetConfigSetting("TempFile") + fileprefix + @"\Output"))
                        {
                            Directory.Delete(ConfigHelper.GetConfigSetting("TempFile") + fileprefix + @"\Output", true);
                        }
                        File.Delete(item);
                    }

                    List<string> InputFilesToProcess = businessFunctions.GetDirectoryFiles(InboundDirectory, DirectoryFileType.InboundFileWithPrefix, fileprefix);
                   
                    foreach (string inputfile in InputFilesToProcess)
                    {
                       if (!inputfile.Contains("inprogress"))
                        {
                           FileInfo fileInfo = new FileInfo(inputfile);
                            string NewPath = diTemp + @"\" + fileInfo.Name;
                            File.Copy(inputfile, NewPath);
                        }
                       Debug.WriteLine("Copy " + inputfile + " to " + diTemp + " folder.");
                       Console.WriteLine("Copy " + inputfile + " to " + diTemp + " folder.");
                    }
                    foreach (FileInfo file in diTemp.GetFiles())
                    {
                          businessFunctions.ConvertToPdf(file);
                          List<string> InboundDirectoryInprogressFiles2 = businessFunctions.GetDirectoryFiles(InboundDirectory, DirectoryFileType.InboundFileWithPrefix, ".inprogress");

                          if (InboundDirectoryInprogressFiles2.Count() > 0)
                          {
                             Main(args);
                             return;
                          }
                    }
                    int CountMsgExtension = Convert.ToInt32(Directory.GetFiles(ConfigHelper.GetConfigSetting("TempFile") + fileprefix, "*.msg").Count());
                    string path = ConfigHelper.GetConfigSetting("TempFile") + fileprefix + @"\" + "Output";
                    DirectoryInfo TempOutPath = new DirectoryInfo(path);
                    int CountTempOutputFiles = Convert.ToInt32(TempOutPath.GetFiles().Count());
                    tempFiles = Directory.GetFiles(ConfigHelper.GetConfigSetting("TempFile") + fileprefix);
                    if (CountTempOutputFiles == Convert.ToInt32(tempFiles.Count()) - CountMsgExtension)
                    {
                        businessFunctions.SaveToArchive(fileprefix);
                        businessFunctions.SaveToProcessed(fileprefix);

                    }
                }
                #endregion
            }

            #endregion

            #region If there is not any inprogress files then it will get prefix of first file and process all file with given FilePreFix
            if (InboundDirectory.GetFiles().Count() > 0)
            {
                FileInfo FirstFile = InboundDirectory.GetFiles()[0];

                string fileprefixMain = businessFunctions.GetFilenamePrefix(FirstFile);

                if (!Directory.Exists(ConfigHelper.GetConfigSetting("TempFile") + fileprefixMain))
                {
                  
                   diTemp = businessFunctions.CreateDirectorywithfolder("TempFile", fileprefixMain, FolderType.folderwithprefix);
                    List<string> InboundPreFixFiles = businessFunctions.GetDirectoryFiles(InboundDirectory, DirectoryFileType.InboundFileWithPrefix, fileprefixMain);
                    foreach (string TempFile in InboundPreFixFiles)
                    {
                        FileInfo TempFileInfo = new FileInfo(TempFile);

                        File.Copy(TempFile, ConfigHelper.GetConfigSetting("TempFile") + fileprefixMain + @"\" + TempFileInfo.Name);
                    }
                }
                if (diTemp != null)
                {
                   List<string> TempxPreFixFiles = businessFunctions.GetDirectoryFiles(diTemp, DirectoryFileType.InboundNormalFiles, "");
                    foreach (string TempxPreFixFile in TempxPreFixFiles)
                    {

                       List<string> InboundDirectoryInprogressFiles2 = businessFunctions.GetDirectoryFiles(InboundDirectory, DirectoryFileType.InboundFileWithPrefix, ".inprogress");

                        if (InboundDirectoryInprogressFiles2.Count() > 0)
                        {
                            Main(args);
                            return;
                        }

                        FileInfo filedd = new FileInfo(TempxPreFixFile);

                        businessFunctions.ConvertToPdf(filedd);
                    }
                    string[] tempSuccessFiles;

                    List<string> InboundDirectoryFiles3 = businessFunctions.GetDirectoryFiles(InboundDirectory, DirectoryFileType.InboundNormalFiles, "");
                   
                    int CountMsgExtension = Convert.ToInt32(Directory.GetFiles(ConfigHelper.GetConfigSetting("TempFile") + fileprefixMain, "*.msg").Count());
                    string path = ConfigHelper.GetConfigSetting("TempFile") + fileprefixMain + @"\" + "Output";

                    DirectoryInfo TempOutPath = new DirectoryInfo(path);
                    int CountTempOutputFiles = Convert.ToInt32(TempOutPath.GetFiles().Count());
                    tempSuccessFiles = Directory.GetFiles(ConfigHelper.GetConfigSetting("TempFile") + fileprefixMain);
                    if (CountTempOutputFiles == Convert.ToInt32(tempSuccessFiles.Count()) - CountMsgExtension)
                    {
                        businessFunctions.SaveToArchive(fileprefixMain);
                        businessFunctions.SaveToProcessed(fileprefixMain);
                    }

                    if (InboundDirectory.GetFiles().Count() > 0)
                    {
                        Main(args);
                    }
                }
            }


            #endregion

            Debug.WriteLine("Process completed..");
        
        }
    }
}
