using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace DSVImportFile
{
   class EmailHelper
   {
      public static void SendMail(string emailBody, string emailSubject, string emailFrom, string emailTo)
      {
         try
         {
            System.Net.Mail.SmtpClient smtpObj = new System.Net.Mail.SmtpClient();
            smtpObj.Port = 25;
            smtpObj.Timeout = 10000;
            smtpObj.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpObj.EnableSsl = true;
            smtpObj.UseDefaultCredentials = false;
            smtpObj.Host = "smtp.gmail.com";
            
            System.Net.Mail.MailMessage mailMessage = new System.Net.Mail.MailMessage();
            System.Net.Mail.MailAddress mailAddrFrom = new System.Net.Mail.MailAddress(emailFrom);
            mailMessage.From = mailAddrFrom;

            string[] addrTo = emailTo.Split(";".ToCharArray());
            foreach (string s in addrTo)
            {
               mailMessage.To.Add(s);
            }

            mailMessage.Subject = emailSubject;    //.GetConfigSetting("MailSubject");
            mailMessage.Body = emailBody + Environment.NewLine + "Time=" + System.DateTime.Now;
            mailMessage.IsBodyHtml = false;

            smtpObj.Credentials = new System.Net.NetworkCredential(ConfigHelper.GetConfigSetting("MailUserID"), ConfigHelper.GetConfigSetting("MailPassword"));

            smtpObj.Send(mailMessage);

         }
         catch (Exception ex)
         {
         }
      }


   }
}
