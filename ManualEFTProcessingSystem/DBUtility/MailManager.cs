using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ManualEFTProcessingSystem.DBUtility
{
    class MailManager
    {
        internal bool SendMail(string frommail, string tomail, string ccmail, string bccmail, string subject, string emailbody)
        {
            bool retVal = true;

            MailMessage _mail = new MailMessage();
            _mail.From = new MailAddress(frommail);

            if (!tomail.Equals(""))
            {
                if (tomail.Contains(";"))
                {
                    string[] toemail_addrs = tomail.Split(';');
                    for (int q = 0; q < toemail_addrs.Length; q++)
                    {
                        _mail.To.Add(toemail_addrs[q]);
                    }
                }
                else
                {
                    _mail.To.Add(tomail);
                }
            }
            else
            {
                return false;
            }


            if (!ccmail.Equals(""))
            {
                if (ccmail.Contains(";"))
                {
                    string[] ccemail_addrs = ccmail.Split(';');
                    for (int q = 0; q < ccemail_addrs.Length; q++)
                    {
                        _mail.CC.Add(ccemail_addrs[q]);
                    }
                }
                else
                {
                    _mail.CC.Add(ccmail);
                }
            }

            if (!bccmail.Equals(""))
            {
                if (bccmail.Contains(";"))
                {
                    string[] bccemail_addrs = bccmail.Split(';');

                    for (int p = 0; p < bccemail_addrs.Length; p++)
                    {
                        _mail.Bcc.Add(bccemail_addrs[p]);
                    }
                }
                else
                {
                    _mail.Bcc.Add(bccmail);
                }
            }

            _mail.Subject = subject;
            _mail.Body = emailbody;
            _mail.IsBodyHtml = true;

            //SmtpClient client = new SmtpClient("mail.mutualtrustbank.com");
            SmtpClient client = new SmtpClient("10.45.2.41"); 
            client.UseDefaultCredentials = false;

            try
            {
                client.Send(_mail);
                return true;
            }
            catch (Exception ex)
            {
                retVal = false;
            }

            return retVal;
        }
    }
}
