using System;
using System.Linq;
using Deanon.db.datamodels.classes.entities;
using Deanon.dumper;
using Deanon.logger;
using Neo4jClient;
using MessageType = Deanon.logger.MessageType;

namespace Deanon.db
{
    public class Neo4JWorker : IDeanonDbWorker
    {
        private const string UrlPattern = "http://{0}:{1}@{2}:{3}/db/data";
        private GraphClient _db;

        private readonly string connectionUri;
        public Neo4JWorker(string address, int port, string user, string password) => this.connectionUri = string.Format(UrlPattern, user, password, address, port);

        public void Connect()
        {
            this._db = new GraphClient(new Uri(this.connectionUri));
            this._db.Connect();
            this.SetUp();
            Logger.Out("Succesfully connected to DB", MessageType.Debug);
        }

        private void SetUp()
        {
            this._db.Cypher
                    .Create("INDEX ON :Person(Id)")
                    .ExecuteWithoutResults();
            this._db.Cypher
                    .Create("INDEX ON :Person(Deleted)")
                    .ExecuteWithoutResults();
        }

        public Node<Person> AddPerson(Person mainPerson)
        {
            Logger.Out("Adding person(Id): {0}", MessageType.Debug, mainPerson.Id);
            return this._db.Cypher
                .Merge("(person:Person { Id: {id} })")
                .OnCreate()
                .Set("person = {person}")
                .WithParams(new
                {
                    id = mainPerson.Id,
                    person = mainPerson
                }).Return(person => person.Node<Person>())
               .Results
               .Single();
        }

        public void AddRelation(Person main, Person friend, EnterType type)
        {
            Logger.Out("Adding relation(Ids): {0} --> {1}", MessageType.Debug, main.Id, friend.Id);
            //create 
            this._db.Cypher
                  .Match("(mainPerson:Person)", "(friendPerson:Person)")
                  .Where((Person mainPerson) => mainPerson.Id == main.Id)
                  .AndWhere((Person friendPerson) => friendPerson.Id == friend.Id)
                  .CreateUnique("mainPerson-[:" + RelationString.ToString(type) + "]->friendPerson")
                  .ExecuteWithoutResults();
        }

        //range 3
        public Person[] GetPeopleFromMinCycles()
        {
            try
            {
                return this._db
                    .Cypher
                    .Match("(a:Person{Deleted:false})-[*3]->(a:Person{Deleted:false})")
                    .ReturnDistinct(a => a.As<Person>())
                    .Results
                    .ToArray();
            }
            catch (Exception ex)
            {
                Logger.Out("Query error: {0}", MessageType.Error, ex.Message);
                return null;
            }
        }

        public void ClearDatabase() => this._db.Cypher
                   .Match("(n)").OptionalMatch("(n)-[r]-()").Delete("n,r")
                    .ExecuteWithoutResults();

        //includes deleted
        public int[] GetAllUsersIds()
        {
            try
            {
                var query = this._db
                    .Cypher
                    .Match("(a)")
                    .ReturnDistinct(a => a.As<Person>().Id);
                var res = query.Results.ToArray();
                return res;
            }
            catch (Exception ex)
            {
                Logger.Out("Query error: {0}", MessageType.Error, ex.Message);
                return null;
            }
        }

        public Person[] GetAllNotDeletedPeople()
        {
            try
            {
                var query = this._db
                    .Cypher
                    .Match("(a:Person{Deleted:false})")
                    .Return(a => a.As<Person>());
                var res = query.Results.ToArray();
                return res;
            }
            catch (Exception ex)
            {
                Logger.Out("Query error: {0}", MessageType.Error, ex.Message);
                return null;
            }
        }

        public Person[] GetUsersRelated(int userId, EnterType type)
        {
            //match (m:Person{Id:169033204})--(f) return f
            try
            {
                var query = this._db
                    .Cypher
                    .Match("(m:Person { Id: {id},Deleted:false})-[:" + RelationString.ToString(type) + "]-(f)")
                    .WithParams(new
                    {
                        id = userId
                    }
                    )
                    .Return(f => f.As<Person>());
                var res = query.Results.ToArray();
                return res;
            }
            catch (Exception ex)
            {
                Logger.Out("Query error: {0}", MessageType.Error, ex.Message);
                return null;
            }
        }

        public Person[] GetHiddenFriendsOfUser(int id)
        {
            var query = this._db
                   .Cypher
                   .Match("(a:Person{Deleted:false})-[:HAVE_FRIEND]->(b:Person{Id:{id},Deleted:false})")
                   .Where("NOT (b)-[:HAVE_FRIEND]->(a)")
                   .WithParams(new
                   {
                       id
                   }
                   )
                   .Return(a => a.As<Person>());
            var res = query.Results.ToArray();
            return res;
        }

        public Person[] GetPeopleWithoutOutRelationsAndNotDeleted()
        {
            //MATCH (a)-->(l:Person{Deleted:false}) WHERE NOT (l:Person{Deleted:false})-->() return l
            //MATCH (a)-->(l:Person{Deleted:false}) WHERE NOT (l:Person{Deleted:false})-->() RETURN l
            try
            {
                var query = this._db
                    .Cypher
                    .Match("(a)-->(l:Person{Deleted:false})")
                    .Where("NOT (l:Person{Deleted:false})-->()")
                    .Return(l => l.As<Person>());
                var res = query.Results.ToArray();
                return res;
            }
            catch (Exception ex)
            {
                Logger.Out("Query error: {0}", MessageType.Error, ex.Message);
                return null;
            }
        }
    }

    /*
	MATCH (a:Person),(b:Person)
WHERE a.name = 'Node A' AND b.name = 'Node B'
CREATE (a)-[r:RELTYPE]->(b)
RETURN r
	*/
}
