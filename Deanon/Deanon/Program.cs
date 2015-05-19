
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Deanon.analyzer;
using Deanon.db;
using Deanon.db.datamodels.classes.entities;
using Deanon.dumper;
using Deanon.logger;
using Deanon.vk;
using Neo4jClient.Cypher;
using VKSharp;


namespace Deanon
{
    class Program
    {
        static void Main(string[] args)
        {
            Main2(args).Wait();
            Console.ReadKey();
        }




        private static VkWorker vkWorker;
        private static VkDumper dumper;
        static DbWorker dbWorker;

        static async Task Main2(string[] args)
        {
            Init();
            /*await dumper.DumpUser(268894603, new DumpingDepth(new List<Depth>()
                {
                    new Depth(EnterType.Friend,0),
                     new Depth(EnterType.Follower, 0),
                     new Depth(EnterType.Post, 1),
                     new Depth(EnterType.Comments, 1),
                    new Depth(EnterType.Likes, 1)
                }));*/



           
            //await InitialDump();
            ExpansionDump();
        }

        static void Init()
        {
            //Logger.AddTypeToUotput(MessageType.Debug);
            Logger.AddTypeToUotput(MessageType.Error);
            Logger.AddTypeToUotput(MessageType.Verbose);

            Console.WriteLine("Enter password:");
            dbWorker = new DbWorker("localhost", 7474, "neo4j", Console.ReadLine());

            List<String> tokens = new List<string>()
            {
                "a63fdf28a13df19652f81b03c638ea05ba0208436641e31b219cd371848c1377a5017430ddef94fe23147"
            };
            vkWorker = new VkWorker(tokens);
            dumper = new VkDumper(dbWorker, vkWorker);


        }


        static async Task InitialDump()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            await dumper.DumpUser(268894603, new DumpingDepth(new List<Depth>()
                {
                    new Depth(EnterType.Friend,3),
                     new Depth(EnterType.Follower, 2),
                     new Depth(EnterType.Post, 1),
                     new Depth(EnterType.Comments, 1),
                    new Depth(EnterType.Likes, 1)
                }));
            sw.Stop();
            Logger.Out("Done in {0} seconds", MessageType.Verbose, sw.ElapsedMilliseconds / 1000);
        }

        private static void ExpansionDump()
        {
            DbGraphAnalyzer analyzer = new DbGraphAnalyzer(dbWorker);
            var people = analyzer.GetPeopleInCycles();

            int maxCount = people.Count();
            int counter = 0;
            foreach (var person in people)
            {
                Logger.Out("Current expansion is {0}%", MessageType.Verbose, 100 / maxCount * (++counter));
                dumper.DumpUser(person.Id, new DumpingDepth(new List<Depth>()
                {
                    new Depth(EnterType.Friend,1),
                     new Depth(EnterType.Follower, 1),
                     new Depth(EnterType.Post, 0),
                     new Depth(EnterType.Comments, 0),
                    new Depth(EnterType.Likes, 0)
                })).Wait();
            }

        }





       /* private Person[] GetFriends()
        {

        }*/


        /*
        var client = new GraphClient(new Uri("http://neo4j:liberty1@localhost:7474/db/data"));
        client.Connect();

        // Create entities
        //var personA = client.Create(new Person() { Name = "Person A" });
        //var personB = client.Create(new Person() { Name = "Person B" });

        //var postA = client.Create(new Post() { PostId = 2323 });
        //var postB = client.Create(new Post() { PostId = 2324 });



        // Create relationships
        client.CreateRelationship(personA, new Posted(postA));
        client.CreateRelationship(personA, new HasPost(postA));

        client.CreateRelationship(personB, new Posted(postB));
        client.CreateRelationship(personA, new HasPost(postB));*/


    }
}
