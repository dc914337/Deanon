
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Deanon.db;
using Deanon.dumper;
using Deanon.logger;
using Deanon.vk;
using Neo4jClient.Cypher;
using VKSharp;

//using VKSharp;

namespace Deanon
{
    class Program
    {
        static void Main(string[] args)
        {
            Main2(args).Wait();
            Logger.Out("All done!", MessageType.Verbose);
            Console.ReadLine();


        }



        static async Task Main2(string[] args)
        {
            //Logger.AddTypeToUotput(MessageType.Debug);
            Logger.AddTypeToUotput(MessageType.Error);
            Logger.AddTypeToUotput(MessageType.Verbose);
        
            DbWorker dbWorker;
            Console.WriteLine("Enter password:");
            dbWorker = new DbWorker("localhost", 7474, "neo4j", Console.ReadLine());

            List<String> tokens = new List<string>()
            {
                "e4db831451dda8a03012f841897231897f1d3f582a85316f67f7586d8bf11f8ae2fe7a4cb751d49bf01ce"
            };
            VkWorker vkWorker = new VkWorker(tokens);


            VkDumper dumper = new VkDumper(dbWorker, vkWorker);

            try
            {
                await dumper.DumpUser(268894603, new DumpingDepth(new List<Depth>()
                {
                    new Depth(EnterType.Friend,2),
                     new Depth(EnterType.Follower, 2),
                     new Depth(EnterType.Post, 1),
                     new Depth(EnterType.Comments, 1),
                    new Depth(EnterType.Likes, 1)
                }));
            }
            catch (Exception ex)
            {
                Logger.Out(ex.Message, MessageType.Error);
                throw;
            }

        }


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
