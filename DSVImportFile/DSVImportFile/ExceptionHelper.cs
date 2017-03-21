using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;

namespace DSVImportFile
{
    public class ExceptionHelper
    {
        #region "static private members"
        static string _emailTo = "";
        static string _emailFrom = "";
        static string _emailSubject = "";
        static string _eventSource = "TTC";
        #endregion


        public void Dispose()
        {
        }


        #region "Constructor"
        static ExceptionHelper()
        {
            _emailTo = ConfigHelper.GetConfigSetting("ExceptionEmailTo");
            _emailFrom = ConfigHelper.GetConfigSetting("ExceptionEmailFrom");
            _emailSubject = ConfigHelper.GetConfigSetting("ExceptionEmailSubject");
            _eventSource = ConfigHelper.GetConfigSetting("ExceptionEventLogSource");
        }
        #endregion

        #region "Public Methods"

        /// <summary>
        /// logs the exceptions and redirects to error page
        /// </summary>
        /// <param name="additionalInfo"></param>
        public static void LogWebException(object sender, UnhandledExceptionEventArgs args)
        {
            HttpContext ctx = HttpContext.Current;
            Exception exception = null;
            exception = (Exception)args.ExceptionObject;
            var st = new StackTrace(exception, true);
            if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
            if (exception == null)
            {
                return;
            }

            if (exception.Message.ToLower().IndexOf("the client disconnected") > -1)
            {
                return;
            }

            System.Collections.Specialized.NameValueCollection exceptInfo = new System.Collections.Specialized.NameValueCollection();
            for (int i = 0; i < st.FrameCount; i++)
            {

                string framed = Convert.ToString(st.GetFrame(i).GetFileName());
                if (!string.IsNullOrEmpty(framed))
                {
                    exceptInfo.Add("", framed);    
                }
            }

            exceptInfo.Add("", string.Format("\n\r\n\r" + "Exception captured at Application_Error event"));



            var sessionData = getSessionData();
            exceptInfo.Add(sessionData);

            ExceptionHelper.LogException(exception, exceptInfo);

            Environment.Exit(1);
        }

        public static void LogException(Exception ex)
        {
            LogException(ex);
        }

        public static void LogException(Exception ex, System.Collections.Specialized.NameValueCollection additionalInfo)
        {
            string exInfo = "";
            if (additionalInfo == null)
            {
                exInfo = formatException(ex);
            }
            else
            {
                exInfo = formatException(ex, additionalInfo);
            }
            if (!string.IsNullOrEmpty(_eventSource))
            {
                logEvent(exInfo);
            }
            if (_emailTo != "")
            {
                EmailHelper.SendMail(exInfo, _emailSubject, _emailFrom, _emailTo);
            }

        }

        public static void LogException(Exception ex, System.Collections.Generic.List<string> lst)
        {

            try
            {
                string exInfo = "";
                exInfo = formatException(ex, lst);

                if (_eventSource != null)
                {
                    logEvent(exInfo);
                }

                if (_emailTo != "")
                {
                    EmailHelper.SendMail(exInfo, _emailSubject, _emailFrom, _emailTo);
                }
            }
            catch (Exception exNon)
            {
            }

        }
        #endregion

        #region "Private Methods"

        //TODO: check this to use logging class
        private static void logEvent(string eventInfo)
        {

            try
            {
                if (!EventLog.SourceExists(_eventSource))
                {
                    EventLog.CreateEventSource(_eventSource, null);
                }

                EventLog myLog = new EventLog();
                myLog.Source = _eventSource;
                myLog.WriteEntry(eventInfo, EventLogEntryType.Error);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private static string formatException(Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Sorry to inform you, but there was a programming exception on a website. Pls see info below:" + Environment.NewLine + Environment.NewLine);
            sb.Append("Server Name=" + Environment.MachineName + Environment.NewLine);
            sb.Append("Exception Info" + Environment.NewLine);
            sb.Append(ex.ToString() + Environment.NewLine);
            return sb.ToString();
        }

        private static string formatException(Exception ex, System.Collections.Specialized.NameValueCollection additionalInfo)
        {

            StringBuilder sb = new StringBuilder();
            sb.Append("Sorry to inform you, but there was a programming exception in application " + _eventSource + ". Pls see info below:"
               + Environment.NewLine + Environment.NewLine);
            sb.Append("Server Name=" + Environment.MachineName + Environment.NewLine);
            sb.Append("Exception Info" + Environment.NewLine);
            sb.Append(ex.ToString() + Environment.NewLine);

            if (additionalInfo != null)
            {
                foreach (string i in additionalInfo)
                {
                    sb.Append(Environment.NewLine + i + " " + additionalInfo.Get(i) + "" + Environment.NewLine);
                }
            }

            return sb.ToString();
        }

        private static string formatException(Exception ex, System.Collections.Generic.List<string> lst)
        {

            StringBuilder sb = new StringBuilder();
            sb.Append("Sorry to inform you, but there was a programming exception on a website. Pls see info below:" + Environment.NewLine + Environment.NewLine);
            sb.Append("Server Name=" + Environment.MachineName + Environment.NewLine);
            sb.Append("Exception Info" + Environment.NewLine);
            sb.Append(ex.ToString() + Environment.NewLine + Environment.NewLine + Environment.NewLine);

            foreach (string s in lst)
            {
                sb.Append(s);
            }

            return sb.ToString();
        }


        private static NameValueCollection getSessionData()
        {
            NameValueCollection result = new NameValueCollection();
            HttpContext ctx = HttpContext.Current;

            try
            {
                result.Add("Caller Info:", "Attempting to determine caller Info");
                StringBuilder sb = new StringBuilder();
                String strHostName2 = Dns.GetHostName();
                Console.WriteLine("Local Machine's Host Name: " + strHostName2);
                // Then using host name, get the IP address list..
                IPHostEntry ipEntry = Dns.GetHostByName(strHostName2);
                IPAddress[] addr = ipEntry.AddressList;
                string ip = addr[0].ToString();

                result.Add("Request.UserHostAddress", ip);

                if (ip.Substring(0, 4) == "10.0")
                {
                    System.Net.IPHostEntry host = null;
                    String strHostName = Dns.GetHostName();
                    host = Dns.GetHostEntry(strHostName);
                    string strComputerName = host.HostName;
                    result.Add("MachineName", strComputerName);
                }

            }
            catch (Exception ex)
            {
            }

            return result;
        }
        #endregion

    }
}
