using System;
using System.Collections;

namespace HolopalmPlus
{
    internal static class ModInstance
    {
        public static HolopalmPlusPlugin instance;

        public static void Log(string message)
        {
            instance.Log(message);
        }

        internal static void StartCoroutine(IEnumerator enumerator)
        {
            throw new NotImplementedException();
        }
    }
}
