using System;
using System.Collections.Generic;

namespace Deanon.logger
{
    internal static class Logger
    {
        private static readonly List<MessageType> ToOutput = new List<MessageType>();

        public static void AddTypeToUotput(MessageType type) => ToOutput.Add(type);

        public static void Out(string message, MessageType type, params object[] parameters)
        {
           if (ToOutput.Contains(type))
            {
                Console.WriteLine(AddPrefix(type, string.Format(message, parameters)));
            }
        }

        private static string AddPrefix(MessageType type, string message) => string.Format("[ {0} ] {1}", type, message);
    }
}
