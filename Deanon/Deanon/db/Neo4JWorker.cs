using System;
using System.Linq;
using Deanon.db.datamodels.classes.entities;
using Deanon.dumper;
using Deanon.logger;
using Neo4jClient;
using VKSharp.Core.Enums;
using MessageType = Deanon.logger.MessageType;

namespace Deanon.db
{
    public class Neo4JWorker : IDeanonDbWorker
    {
        private const String UrlPattern = "http://{0}:{1}@{2}:{3}/db/data";
        private GraphClient _db;


        private readonly string connectionUri;
        public Neo4JWorker(String address, int port, String user, String password)
        {
            this.connectionUri = String.Format(UrlPattern, user, password, address, port);
        }

        public void Connect()
        {
            _db = new GraphClient(new Uri(connectionUri));
            _db.Connect();
            SetUp();
            Logger.Out("Succesfully connected to DB", MessageType.Debug);
        }

        private void SetUp()
        {
            _db.Cypher
                    .Create("INDEX ON :Person(Id)")
                    .ExecuteWithoutResults();
        }


        public Node<Person> AddPerson(Person mainPerson)
        {
            Logger.Out("Adding person to DB: {0}", MessageType.Debug, mainPerson.Url);
            return _db.Cypher
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
            Logger.Out("Adding relation to DB: {0} --> {1}", MessageType.Debug, main.Url, friend.Url);
            //create 
            _db.Cypher
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

                var query = _db
                    .Cypher
                    .Match("(a)-[*3]->(a)")
                    .ReturnDistinct(a => a.As<Person>());
                var res = query.Results.ToArray();
                return res;
            }
            catch (Exception ex)
            {
                Logger.Out("Query error: {0}", MessageType.Error, ex.Message);
                return null;
            }

        }


        public int[] GetAllUsersIds()
        {
            try
            {

                var query = _db
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

        public Person[] GetAllPeople()
        {
            try
            {
                var query = _db
                    .Cypher
                    .Match("(a)")
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
                var query = _db
                    .Cypher
                    .Match("(m:Person { Id: {id} })-[:" + RelationString.ToString(type) + "]-(f)")
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

        public Person[] GetPeopleWithoutOutRelations()
        {
            //MATCH (a)-->(l) WHERE NOT (l)-->() return l
            try
            {
                var query = _db
                    .Cypher
                    .Match("(a)-->(l)")
                    .Where("NOT (l)-->()")
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
