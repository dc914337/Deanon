using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Deanon.db;
using Deanon.db.datamodels.classes.entities;
using Deanon.dumper;

namespace Deanon.analyzer
{
    class DbGraphAnalyzer
    {
        private IDeanonDbWorker _neo4J;
        public DbGraphAnalyzer(IDeanonDbWorker neo4J)
        {
            _neo4J = neo4J;
        }

        public Person[] GetPeopleInCycles()
        {
            var v = _neo4J.GetPeopleFromMinCycles().GroupBy(a => a.Id).Select(b => b.First()).ToArray();
            return v;
        }

        public Person[] GetPeopleWithoutOutRelations()
        {
            var v = _neo4J.GetPeopleWithoutOutRelations().GroupBy(a => a.Id).Select(b => b.First()).ToArray();
            return v;
        }

        public Person[] GetUsersFriends(int userId)
        {
            return _neo4J.GetUsersRelated(userId, EnterType.Friend).GroupBy(a => a.Id).Select(b => b.First()).ToArray();
        }
        public Person[] GetAllPeople()
        {
            var v = _neo4J.GetAllPeople().GroupBy(a => a.Id).Select(b => b.First()).ToArray();
            return v;
        }
    }



}
