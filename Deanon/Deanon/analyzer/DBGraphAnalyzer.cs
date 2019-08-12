using System.Linq;
using Deanon.db;
using Deanon.db.datamodels.classes.entities;
using Deanon.dumper;

namespace Deanon.analyzer
{
    public class DbGraphAnalyzer
    {
        private readonly IDeanonDbWorker _neo4J;

        public DbGraphAnalyzer(IDeanonDbWorker neo4J) => this._neo4J = neo4J;

        public Person[] GetPeopleInCycles() => this._neo4J.GetPeopleFromMinCycles().GroupBy(a => a.Id).Select(b => b.First()).ToArray();

        public Person[] GetPeopleWithoutOutRelations() => this._neo4J.GetPeopleWithoutOutRelationsAndNotDeleted().GroupBy(a => a.Id).Select(b => b.First()).ToArray();

        public Person[] GetUsersFriends(int userId) => this._neo4J.GetUsersRelated(userId, EnterType.Friend).GroupBy(a => a.Id).Select(b => b.First()).ToArray();

        public Person[] GetAllPeople() => this._neo4J.GetAllNotDeletedPeople().GroupBy(a => a.Id).Select(b => b.First()).ToArray();
    }
}
