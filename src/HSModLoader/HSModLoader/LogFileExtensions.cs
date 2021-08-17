using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSModLoader
{
    /// <summary>
    /// Used to send information to an error log file.
    /// </summary>
    public static class LogFileExtensions
    {
        private static readonly string ErrorLog = "error.log";

        public static void AppendToLogFile(this Exception e)
        {
            File.AppendAllText(ErrorLog, string.Format("\n\n[{0}]\n{1}\n{2}", DateTime.Now.ToString(), e.Message, e.StackTrace));
        }

        public static void AppendToLogFile(this string s)
        {
            File.AppendAllText(ErrorLog, string.Format("\n\n[{0}]\n{1}", DateTime.Now.ToString(), s));
        }

    }
}
