using kasthack.vksharp;
using System.Collections.Generic;

namespace Deanon.dumper.vk
{
    public class TokenRepository
    {
        private readonly List<Token> tokens;

        private int tokenPointer;

        public TokenRepository()
        {
            this.tokenPointer = -1;
            this.tokens = new List<Token>();
        }

        public bool ReadFromFile(string path) => false;

        public void AddToken(string token) => this.tokens.Add(new Token(token));

        public Token GetToken()
        {
            this.tokenPointer++;
            if (this.tokenPointer >= this.tokens.Count)
            {
                this.tokenPointer = 0;
            }

            return this.tokens[this.tokenPointer];
        }
    }
}
