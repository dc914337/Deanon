using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Deanon.db;
using Deanon.db.datamodels.classes.entities;

namespace Deanon.analyzer
{
    class DbGraphAnalyzer
    {
        private DbWorker _db;
        public DbGraphAnalyzer(DbWorker db)
        {
            _db = db;
        }

        public Person[] GetPeopleInCycles()
        {
            var v = _db.GetPeopleFromMinCycles().GroupBy(a => a.Id).Select(b => b.First()).ToArray();
            return v;
        }

    }



}
