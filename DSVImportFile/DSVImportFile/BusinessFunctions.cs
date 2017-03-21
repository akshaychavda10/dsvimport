using MsgReader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Configuration;
using System.Diagnostics;

namespace DSVImportFile
{
   public class BusinessFunctions
   {
      DirectoryInfo diTempoutput;

      #region Fields
      /// <summary>
      /// Used to track all the created temporary folders
      /// </summary>
      readonly List<string> _tempFolders = new List<string>();
      #endregion

      #region Function to get filename prefix to processed that all files whhich contains that fileprefix
      public string GetFilenamePrefix(FileInfo fileinfo)
      {
         string fileprefix = string.Empty;
         if (fileinfo.Name.Contains("_"))
         {
            string[] InvoicePO = fileinfo.Name.Split('_');
            fileprefix = InvoicePO[0] + "_" + InvoicePO[1];
            if (InvoicePO[1].Contains("."))
            {
               string[] InvoicePO21 = fileprefix.Split('.');
               fileprefix = InvoicePO21[0];
            }

         }
         return fileprefix;
      }

      #endregion

      #region function which will call Cloud convert API and managed responce
      public async Task ConvertFromDoc(CloudConverter cloudConverter, FileInfo fileinfo)
      {
         try
         {
            using (HttpClient client = new HttpClient())
            {
               client.BaseAddress = new Uri(ConfigHelper.GetConfigSetting("BaseAddress"));
               client.DefaultRequestHeaders.Accept.Clear();
               client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www.form-urlencoded"));
               StringContent content = new StringContent(JsonConvert.SerializeObject(cloudConverter), Encoding.UTF8, "application/json");
               Debug.WriteLine(fileinfo.Name + " is start to converting.");
               Console.WriteLine(fileinfo.Name + " is start to converting.");
               HttpResponseMessage response = client.PostAsync(client.BaseAddress, content).Result;
               if (response.StatusCode == HttpStatusCode.OK)
               {
                 
                  System.Uri url = response.RequestMessage.RequestUri;
                  string fileprefix = GetFilenamePrefix(fileinfo);
                  if (!Directory.Exists(ConfigHelper.GetConfigSetting("TempFile") + fileprefix))
                  {
                     DirectoryInfo diTemp = CreateDirectorywithfolder("TempFile", fileprefix, FolderType.folderwithprefix);
                     diTempoutput = CreateDirectorywithfolder("TempFile", fileprefix, FolderType.folderwithoutput);
                  }
                  else if (!Directory.Exists(ConfigHelper.GetConfigSetting("TempFile") + fileprefix + @"\Output"))
                  {
                     diTempoutput = CreateDirectorywithfolder("TempFile", fileprefix, FolderType.folderwithoutput);
                  }
                  string fileName = ConfigHelper.GetConfigSetting("TempFile") + fileprefix + @"\Output\"
                                           + Path.GetFileNameWithoutExtension(cloudConverter.filename) + ".pdf";
                  string[] InputFiles = Directory.GetFiles(ConfigHelper.GetConfigSetting("InputFile"));

                  foreach (string file in InputFiles)
                  {
                     if (file.Contains("inprogress"))
                     {
                        File.Delete(file);
                        break;
                     }
                  }
                  HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                  using (WebResponse responsestream = (HttpWebResponse)request.GetResponse())
                  {
                     using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                     {
                        byte[] bytes = ReadFully(responsestream.GetResponseStream());
                        stream.Write(bytes, 0, bytes.Length);
                     }
                  }

                  Console.WriteLine(fileinfo.Name + " is converted.");
               }
               else
               {
                  Console.WriteLine(fileinfo.Name + " is fail to convert.");
                  string fileprefix = GetFilenamePrefix(fileinfo);
                  int Count = GetErrorCount(fileprefix);
                  string filename = cloudConverter.filename;

                  filename = fileprefix + ".inprogress." + Count + ".txt";
                  if (!File.Exists(ConfigHelper.GetConfigSetting("InputFile") + filename))
                  {
                     int Counter = Count + 1;
                     filename = fileprefix + ".inprogress." + Counter + ".txt";
                     using (var myFile = File.Create(ConfigHelper.GetConfigSetting("InputFile") + filename))
                     {
                        // interact with myFile here, it will be disposed automatically
                     }
                  }
                  else
                  {
                     Count = Count + 1;
                     if (Count > 3)
                     {
                        string[] Templfiles = Directory.GetFiles(ConfigHelper.GetConfigSetting("TempFile") + fileprefix);
                        foreach (string file in Templfiles)
                        {
                           FileInfo Processedfile = new FileInfo(file);
                           File.Move(file, ConfigHelper.GetConfigSetting("ProcessErrorFile") + Processedfile.Name);
                        }

                        //Getting Inbound Directory to coonvert file from Inbound
                        DirectoryInfo InboundDirectory = new DirectoryInfo(ConfigHelper.GetConfigSetting("InputFile"));
                        List<string> InputFilesToProcess = GetDirectoryFiles(InboundDirectory, DirectoryFileType.InboundFileWithPrefix, fileprefix);
                        foreach (string item in InputFilesToProcess)
                        {
                           FileInfo Processedfile = new FileInfo(item);
                           File.Delete(item);
                        }
                     }
                     File.Delete(ConfigHelper.GetConfigSetting("InputFile") + fileprefix + ".inprogress." + (Count - 1) + ".txt");
                     using (var myFile = File.Create(ConfigHelper.GetConfigSetting("InputFile") + fileprefix + ".inprogress." + (Count) + ".txt"))
                     {
                     }
                  }

               }
            }
         }
         catch (Exception ex)
         {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            UnhandledExceptionEventArgs arges = new UnhandledExceptionEventArgs(ex,false);
            ExceptionHelper exceptionHelper = new ExceptionHelper();
            ExceptionHelper.LogWebException(null, arges);

         }

      }
      #endregion

      #region Function to get list of files for given directory
      public List<string> GetDirectoryFiles(DirectoryInfo InboundDirectory, DirectoryFileType FileType, string Fileprefix)
      {
         switch (FileType)
         {
            case DirectoryFileType.InboundNormalFiles:
               var DirectoryFiles = from f in InboundDirectory.EnumerateFiles().ToList()
                                    select f.FullName;
               return DirectoryFiles.ToList();
            case DirectoryFileType.InboundFileWithPrefix:

               var InboundDirectoryFiles = from f in InboundDirectory.EnumerateFiles().ToList()
                                           select f.FullName;
               var InputFilesToProcess = from p in InboundDirectoryFiles.ToList()
                                         where p.Contains(Fileprefix)
                                         select p;
               return InputFilesToProcess.ToList();
         }
         return null;
      }

      #endregion

      #region Function to create file from streame
      public static byte[] ReadFully(Stream input)
      {
         byte[] buffer = new byte[16 * 1024];
         using (MemoryStream ms = new MemoryStream())
         {
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
               ms.Write(buffer, 0, read);
            }
            return ms.ToArray();
         }
      }
      #endregion

      #region Function to create TemporaryFolder
      /// <summary>
      /// Returns a temporary folder
      /// </summary>
      /// <returns></returns>
      public string GetTemporaryFolder(string filedetail)
      {
         FileInfo fileInfo = new FileInfo(filedetail);
         string fileprefix = GetFilenamePrefix(fileInfo);
         string tempDirectory = string.Empty;

         if (!Directory.Exists(ConfigHelper.GetConfigSetting("TempFile") + fileprefix))
         {
            tempDirectory = Directory.CreateDirectory(ConfigHelper.GetConfigSetting("TempFile") + fileprefix).ToString();
         }

         tempDirectory = ConfigHelper.GetConfigSetting("TempFile") + fileprefix.ToString();
         return tempDirectory;
      }
      #endregion

      #region function to Extract Files From Outlook Msg
      /// <summary>
      /// Opens the selected MSG of EML file
      /// </summary>
      /// <param name="fileName"></param>
      private void ExtractFilesFromOutlookMsg(string fileName, string fileprefix)
      {
         // Open the selected file to read.
         string tempFolder = null;
            tempFolder = GetTemporaryFolder(fileName);
            _tempFolders.Add(tempFolder);

            var msgReader = new Reader();

            var files = msgReader.ExtractToFolder(
                fileName,
                tempFolder,
                false);

            var error = msgReader.GetErrorMessage();

            if (!string.IsNullOrEmpty(error))
               throw new Exception(error);
            foreach (string Msgitem in files)
            {
               FileInfo fileInfo = new FileInfo(Msgitem);
               if (fileInfo.Extension.ToUpper() == ".HTM")
               {
                  string newPath = Path.ChangeExtension(Msgitem, "html");
                  string oldfilename = fileInfo.FullName;
                  string newFilename = ConfigHelper.GetConfigSetting("TempFile") + fileprefix + fileInfo.Name;
                  File.Move(oldfilename, newFilename);
                  string nepath = ConfigHelper.GetConfigSetting("TempFile") + fileprefix + @"\" + fileprefix + "_" + fileInfo.Name;
                  File.Move(newFilename, nepath);
                  FileInfo ChangedfileInfo = new FileInfo(nepath);
                  ConvertToPdf(ChangedfileInfo);
               }
               else
               {
                  ConvertToPdf(fileInfo);
               }
            }
      }
      #endregion

      #region  function to take File and Convert it to pdf
      public void ConvertToPdf(FileInfo file)
      {
         string ext = Path.GetExtension(file.Extension);
         if (Convert.ToString(ext).ToUpper() == ".MSG")
         {
            string fileprefix = GetFilenamePrefix(file);
            this.ExtractFilesFromOutlookMsg(file.FullName, fileprefix);
         }
         else if (Convert.ToString(ext).ToUpper() == ".PDF"
                                 || Convert.ToString(ext).ToUpper() == ".JPG"
                                 || Convert.ToString(ext).ToUpper() == ".JPEG"
                                 || Convert.ToString(ext).ToUpper() == ".PNG"
                                 || Convert.ToString(ext).ToUpper() == ".GIF"
                                 || Convert.ToString(ext).ToUpper() == ".TIF"
                                 || Convert.ToString(ext).ToUpper() == ".BMP")
         {
            string fileprefix = GetFilenamePrefix(file);
            diTempoutput = new DirectoryInfo(ConfigHelper.GetConfigSetting("TempFile") + fileprefix + @"\Output\");
            if (!Directory.Exists(ConfigHelper.GetConfigSetting("TempFile") + fileprefix + @"\Output"))
            {
               diTempoutput = CreateDirectorywithfolder("TempFile", fileprefix, FolderType.folderwithoutput);
            }

            File.Copy(file.FullName, diTempoutput.FullName + @"\" + file.Name);
         }
         else
         {
            Byte[] bytes = File.ReadAllBytes(file.FullName);
            String filebytestring = Convert.ToBase64String(bytes);
            CloudConverter cloudConverter = new CloudConverter();
            cloudConverter.file = filebytestring;
            cloudConverter.apikey = ConfigHelper.GetConfigSetting("apikey");
            cloudConverter.inputformat = Convert.ToString(ext).Replace(".", "");
            cloudConverter.outputformat = "pdf";
            cloudConverter.input = ConfigHelper.GetConfigSetting("input");
            cloudConverter.filename = file.Name;
            cloudConverter.timeout = "30000";
            cloudConverter.wait = ConfigHelper.GetConfigSetting("wait");
            cloudConverter.download = "inline";
            cloudConverter.save = "true";
            
            ConvertFromDoc(cloudConverter, file);
         }

      }
      #endregion

      #region Function  to Create folder for Archive files and Move files to Archive folder
      public void SaveToArchive(string fileprefix)
      {
         string[] InputFiles = Directory.GetFiles(ConfigHelper.GetConfigSetting("InputFile"));
         foreach (string file in InputFiles)
         {
            if (file.Contains("inprogress"))
            {
               File.Delete(file);
            }
            else if (file.Contains(fileprefix))
            {
               FileInfo fileArchived = new FileInfo(file);
               DirectoryInfo diTemp = new DirectoryInfo(ConfigHelper.GetConfigSetting("Archive") + DateTime.Now.Date.ToString("yyMMdd") + @"\" + fileprefix);
               if (!Directory.Exists(diTemp.ToString()))
               {
                  diTemp = Directory.CreateDirectory(ConfigHelper.GetConfigSetting("Archive") + DateTime.Now.Date.ToString("yyMMdd") + @"\" + fileprefix);
               }
               File.Move(file, ConfigHelper.GetConfigSetting("Archive") + DateTime.Now.Date.ToString("yyMMdd") + @"\" + fileprefix + @"\" + fileArchived.Name);
               Debug.WriteLine("Moved " + fileArchived.Name + " to Archive folder.");
            }
         }
      }
      #endregion

      #region Function Create folder for Processed files and Move files to Processed folder if all files processed sucessfuly
      public void SaveToProcessed(string fileprefix)
      {
         string[] Outputfiles = Directory.GetFiles(ConfigHelper.GetConfigSetting("TempFile") + fileprefix + @"\Output");
         foreach (string file in Outputfiles)
         {
            FileInfo fileProcessed = new FileInfo(file);
            DirectoryInfo diTemp = new DirectoryInfo(ConfigHelper.GetConfigSetting("Processed"));
            if (!Directory.Exists(diTemp.ToString()))
            {
               diTemp = Directory.CreateDirectory(ConfigHelper.GetConfigSetting("Processed"));
            }
            File.Move(file, ConfigHelper.GetConfigSetting("Processed") + fileProcessed.Name);
            Debug.WriteLine("Moved " + fileProcessed.Name + " to Processed folder.");
         }

      }
      #endregion

      #region Function to get count of error occurence at the time of convertion
      public int GetErrorCount(string filePreFix)
      {
         DirectoryInfo InboundDirectory = new DirectoryInfo(ConfigHelper.GetConfigSetting("InputFile"));
         List<string> InboundDirectoryInprogressFiles = GetDirectoryFiles(InboundDirectory, DirectoryFileType.InboundFileWithPrefix, ".inprogress");

         if (InboundDirectoryInprogressFiles.Count() > 0)
         {
            FileInfo Inboundfile = new FileInfo(InboundDirectoryInprogressFiles.FirstOrDefault().ToString());
            if (Inboundfile.Name.Contains(".inprogress."))
            {
               int ErrorOccurence = 1;
               string[] filenameParts = Inboundfile.Name.Split('.');
               return ErrorOccurence = Convert.ToInt32(filenameParts[filenameParts.Length - 2]);
            }
         }
         return 0;
      }
      #endregion

      #region Function to create directory with given name
      public DirectoryInfo CreateDirectorywithfolder(string name, string fileprefix, FolderType foldertype)
      {
         DirectoryInfo diTemp = null;
         switch (foldertype)
         {
            case FolderType.onlyfolder:
               diTemp = Directory.CreateDirectory(ConfigHelper.GetConfigSetting(name));
               break;
            case FolderType.folderwithprefix:
               diTemp = Directory.CreateDirectory(ConfigHelper.GetConfigSetting(name) + fileprefix);
               break;
            case FolderType.folderwithoutput:
               diTemp = Directory.CreateDirectory(ConfigHelper.GetConfigSetting(name) + fileprefix + @"/Output");
               break;
         }
         return diTemp;
      }
      #endregion

   }
}
