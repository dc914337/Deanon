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
    public class DbWorker
    {
        private const String UrlPattern = "http://{0}:{1}@{2}:{3}/db/data";
        private GraphClient _db;


        private readonly string connectionUri;
        public DbWorker(String address, int port, String user, String password)
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
            if (!_db.CheckIndexExists("personIdIndex", IndexFor.Node))
            {
                _db.CreateIndex("personIdIndex", new IndexConfiguration() { Provider = IndexProvider.lucene, Type = IndexType.exact }, IndexFor.Node);
            }

            if (!_db.CheckIndexExists("relationshipNameIndex", IndexFor.Relationship))
            {
                _db.CreateIndex("relationshipNameIndex", new IndexConfiguration() { Provider = IndexProvider.lucene, Type = IndexType.exact }, IndexFor.Relationship);
            }

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


        public Node<Person> AddPotentialFriend(Person main, Person friend, EnterType type)
        {
            Logger.Out("Adding potential friend to DB: {0}", MessageType.Debug, friend.Url);

            var friendNode = _db.Cypher
             .Merge("(person:Person { Id: {id} })")
             .OnCreate()
             .Set("person = {friend}")
             .WithParams(new
             {
                 id = friend.Id,
                 friend
             }).Return(person => person.Node<Person>())
             .Results
             .Single();

            //create 
            _db.Cypher
                  .Match("(mainPerson:Person)", "(friendPerson:Person)")
                  .Where((Person mainPerson) => mainPerson.Id == main.Id)
                  .AndWhere((Person friendPerson) => friendPerson.Id == friend.Id)
                  .CreateUnique("mainPerson-[:" + RelationString.ToString(type) + "]->friendPerson")
                  .ExecuteWithoutResults();

            return friendNode;
        }


        //range 3
        public Person[] GetPeopleFromMinCycles()
        {
            try
            {

                var query = _db
                    .Cypher
                    .Match("(a)-[*3]->(a)")
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
    }

    /*
	MATCH (a:Person),(b:Person)
WHERE a.name = 'Node A' AND b.name = 'Node B'
CREATE (a)-[r:RELTYPE]->(b)
RETURN r
	*/
}
