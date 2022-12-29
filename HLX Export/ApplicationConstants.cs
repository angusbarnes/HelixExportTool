
using System.IO;

namespace HLXExport
{
    // These values should all be constants and should be checked before every release version of the software
    public static class ApplicationConstants
    {
        public const string SOFTWARE_VERSION = "v0.1.21a";
        public const string SOFTWARE_NAME = "HLX Export";
        public const string FORMATTED_TITLE = SOFTWARE_NAME + " " + SOFTWARE_VERSION;
        public const string SOFTWARE_SUPPORT_LINK = "mailto:angusbrns@gmail.com";
        public static readonly string DEFAULT_PROFILE_PATH = Path.Join(Directory.GetCurrentDirectory(), "\\profiles");
        public const string TEMP_DATA_PATH = ".\\temp\\";
#if DEBUG
        public const bool DEBUG_MODE_ENABLED = true;
#endif
#if !DEBUG 
        public const bool DEBUG_MODE_ENABLED = true;
#endif
    }
}
