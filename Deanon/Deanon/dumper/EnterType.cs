using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deanon.dumper
{
    public enum EnterType
    {
        Friend,
        Post,
        Comments,
        Follower,
        Likes
    }

    public static class RelationString
    {
        private static Dictionary<EnterType, String> relationsDictionary;
        static RelationString()
        {
            relationsDictionary = new Dictionary<EnterType, string>()
            {
                { EnterType.Friend, "HAVE_FRIEND" },
                { EnterType.Post, "HAVE_POST_FROM" },
                { EnterType.Comments, "HAVE_COMMENT_FROM" },
                { EnterType.Follower, "HAVE_FOLLOWER" },
                { EnterType.Likes, "HAVE_LIKE" }
            };
        }

        public static String ToString(EnterType type)
        {
            return relationsDictionary[type];
        }
    }
}
