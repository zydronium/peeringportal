using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LUJEWebsite.Mailer
{
    internal class Configuration
    {
        public static string DBPath
        {
            get
            {
                string server = Environment.GetEnvironmentVariable("DATABASE_SERVER");
                string user = Environment.GetEnvironmentVariable("DATABASE_USER");
                string password = Environment.GetEnvironmentVariable("DATABASE_PASSWORD");
                string name = Environment.GetEnvironmentVariable("DATABASE_NAME");
                return String.Format("Server={0};Database={1};Uid={2};Pwd={3};", server, name, user, password);
            }
        }
        public static string MailServer
        {
            get
            {
                return Environment.GetEnvironmentVariable("MAIL_SERVER");
            }
        }
        public static int MailPort
        {
            get
            {
                return Convert.ToInt32(Environment.GetEnvironmentVariable("MAIL_PORT"));
            }
        }
        public static string MailUser
        {
            get
            {
                return Environment.GetEnvironmentVariable("MAIL_USER");
            }
        }
        public static string MailPassword
        {
            get
            {
                return Environment.GetEnvironmentVariable("MAIL_PASSWORD");
            }
        }
        public static string MailFrom
        {
            get
            {
                return Environment.GetEnvironmentVariable("MAIL_FROM");
            }
        }
    }
}
