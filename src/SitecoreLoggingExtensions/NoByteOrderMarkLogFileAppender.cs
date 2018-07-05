using System.Text;
using log4net.Appender;

namespace SitecoreLoggingExtensions
{
    public class NoByteOrderMarkLogFileAppender : SitecoreLogFileAppender
    {
        protected override void OpenFile(string fileName, bool append)
        {
            // Do not emit BOM
            Encoding = new UTF8Encoding(false);

            base.OpenFile(fileName, append);
        }
    }
}