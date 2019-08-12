using System.Collections.Generic;
using Deanon.logger;

namespace Deanon.dumper.cache
{
    public class Cache
    {
        private readonly HashSet<int> peopleInBaseIds;

        public Cache() => this.peopleInBaseIds = new HashSet<int>();

        public void AddManyIds(int[] ids)
        {
            foreach (var id in ids)
            {
                this.AddPersonId(id);
            }
        }

        public void AddPersonId(int id)
        {
            if (!this.CheckContainsPersonId(id))
            {
                this.peopleInBaseIds.Add(id);
            }
        }

        public bool CheckAddPersonId(int id)
        {
            var contains = this.CheckContainsPersonId(id);
            this.peopleInBaseIds.Add(id);
            return contains;
        }

        public bool CheckContainsPersonId(int id)
        {
            var contains = this.peopleInBaseIds.Contains(id);
            Logger.Out($"Cache {(contains ? "hit" : "miss")}! {{0}}", MessageType.DebugCache, id);
            return contains;
        }
    }
}
