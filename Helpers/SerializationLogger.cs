namespace DaSerialization.Internal
{
    public static class SerializationLogger
    {
        public static void Log(string message)
        {
#if UNITY_2018_1_OR_NEWER
            UnityEngine.Debug.Log(message);
#else
            System.Console.WriteLine("INFO: " + message);
#endif
        }

        public static void LogWarning(string warning)
        {
#if UNITY_2018_1_OR_NEWER
            UnityEngine.Debug.LogWarning(warning);
#else
            System.Console.WriteLine("WARNING: " + warning);
#endif
        }

        public static void LogError(string error)
        {
#if UNITY_2018_1_OR_NEWER
            UnityEngine.Debug.LogError(error);
#else
            System.Console.WriteLine("ERROR: " + error);
#endif
        }
    }
}