using System;
using System.Collections.Generic;
using VKSharp.Data.Api;

namespace Deanon.dumper.vk
{
    class VkTokenRepository
    {
        private List<VKToken> tokens;

        private int tokenPointer;

        public VkTokenRepository()
        {
            tokenPointer = -1;
            tokens = new List<VKToken>();
        }

        public bool ReadFromFile(String path)
        {
            return false;
        }

        public void AddToken(String token)
        {
            tokens.Add(new VKToken(token));
        }

        public VKToken GetToken()
        {
            tokenPointer++;
            if (tokenPointer >= tokens.Count)
                tokenPointer = 0;
            return tokens[tokenPointer];
        }

    }
}
