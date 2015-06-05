using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Deanon.logger;

namespace Deanon.dumper.cache
{
    internal class Cache
    {
        private HashSet<int> _peopleInBaseIds;

        public Cache()
        {
            _peopleInBaseIds = new HashSet<int>();
        }

        public void AddManyIds(int[] ids)
        {
            foreach (var id in ids)
            {
                AddPersonId(id);
            }

        }

        public void AddPersonId(int id)
        {
            if (!CheckContainsPersonId(id))
                _peopleInBaseIds.Add(id);
        }

        public bool CheckAddPersonId(int id)
        {
            bool contains = CheckContainsPersonId(id);
            if (!contains)
                _peopleInBaseIds.Add(id);
            return contains;
        }

        public bool CheckContainsPersonId(int id)
        {
            bool contains = _peopleInBaseIds.Contains(id);
            if (contains)
                Logger.Out("Cache hit! {0}", MessageType.DebugCache, id);
            else
                Logger.Out("Cache miss! {0}", MessageType.DebugCache, id);

            return contains;
        }
    }
}
