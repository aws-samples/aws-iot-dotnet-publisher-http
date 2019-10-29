using System.Reflection;
using log4net;

namespace Awsiotdevicepubhttp.utils
{
    public static class Logger
    {
        private static readonly ILog log =
           LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void LogInfo(string message)
        {
            log.Info(message);
        }

        public static void LogDebug(string message)
        {
            log.Debug(message);
        }


        public static void LogError(string message)
        {
            log.Error(message);
        }


        public static void LogFatal(string message)
        {
            log.Fatal(message);
        }


        public static void LogWarn(string message)
        {
            log.Warn(message);
        }
    }
}