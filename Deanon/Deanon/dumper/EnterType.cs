using System.Collections.Generic;

namespace Deanon.dumper
{
    public static class RelationString
    {
        private static readonly Dictionary<EnterType, string> relationsDictionary;

        static RelationString() => relationsDictionary = new Dictionary<EnterType, string>()
            {
                { EnterType.Friend, "HAVE_FRIEND" },
                { EnterType.Post, "HAVE_POST_FROM" },
                { EnterType.Comments, "HAVE_COMMENT_FROM" },
                { EnterType.Follower, "HAVE_FOLLOWER" },
                { EnterType.Likes, "HAVE_LIKE" }
            };

        public static string ToString(EnterType type) => relationsDictionary[type];
    }
}
