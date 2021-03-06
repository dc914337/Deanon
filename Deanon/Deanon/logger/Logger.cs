﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deanon.logger
{
    static class Logger
    {
        private static readonly List<MessageType> ToOutput = new List<MessageType>();

        public static void AddTypeToUotput(MessageType type)
        {
            ToOutput.Add(type);
        }

        public static void Out(String message, MessageType type, params Object[] parameters)
        {
           if (ToOutput.Contains(type))
                Console.WriteLine(AddPrefix(type, String.Format(message, parameters)));
        }

        private static String AddPrefix(MessageType type, String message)
        {
            return String.Format("[ {0} ] {1}", type, message);
        }
    }
}
