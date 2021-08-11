using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSModLoader.App
{
    /// <summary>
    /// Used to send caught Exceptions to an error log file.
    /// </summary>
    public static class LogFileExtensions
    {
        public static readonly string ErrorLog = "error.log";

        public static void AppendToLogFile(this Exception e)
        {
            File.AppendAllText(ErrorLog, string.Format("\n\n[{0}]\n{1}\n{2}", DateTime.Now.ToString(), e.Message, e.StackTrace));
        }

    }
}
