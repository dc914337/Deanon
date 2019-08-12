using System;
using System.Linq;
using Deanon.db.datamodels.classes.entities;
using Deanon.dumper;
using Deanon.logger;
using Neo4jClient;
using MessageType = Deanon.logger.MessageType;

namespace Deanon.db
{
    public class DbWorker
    {
        private const string urlPattern = "http://{0}:{1}@{2}:{3}/db/data";
        private GraphClient db;

        private readonly string connectionUri;
        public DbWorker(string address, int port, string user, string password) => this.connectionUri = string.Format(urlPattern, user, password, address, port);

        public void Connect()
        {
            this.db = new GraphClient(new Uri(this.connectionUri));
            this.db.Connect();
            this.SetUp();
            Logger.Out("Succesfully connected to DB", MessageType.Debug);
        }

        private void SetUp()
        {
            if (!this.db.CheckIndexExists("personIdIndex", IndexFor.Node))
            {
                this.db.CreateIndex("personIdIndex", new IndexConfiguration() { Provider = IndexProvider.lucene, Type = IndexType.exact }, IndexFor.Node);
            }

            if (!this.db.CheckIndexExists("relationshipNameIndex", IndexFor.Relationship))
            {
                this.db.CreateIndex("relationshipNameIndex", new IndexConfiguration() { Provider = IndexProvider.lucene, Type = IndexType.exact }, IndexFor.Relationship);
            }
        }

        public Node<Person> AddPerson(Person mainPerson)
        {
            Logger.Out("Adding person to DB: {0}", MessageType.Debug, mainPerson.Url);
            return this.db.Cypher
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

            var friendNode = this.db.Cypher
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
            this.db.Cypher
                  .Match("(mainPerson:Person)", "(friendPerson:Person)")
                  .Where((Person mainPerson) => mainPerson.Id == main.Id)
                  .AndWhere((Person friendPerson) => friendPerson.Id == friend.Id)
                  .CreateUnique("mainPerson-[:" + RelationString.ToString(type) + "]->friendPerson")
                  .ExecuteWithoutResults();

            return friendNode;
        }
    }

    /*
	MATCH (a:Person),(b:Person)
WHERE a.name = 'Node A' AND b.name = 'Node B'
CREATE (a)-[r:RELTYPE]->(b)
RETURN r
	*/
}
