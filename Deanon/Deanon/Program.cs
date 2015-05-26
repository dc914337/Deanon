
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
using Deanon.dumper.vk;
using Deanon.logger;
using Neo4jClient.Cypher;
using VKSharp;


namespace Deanon
{
    class Program
    {
        private static IDeanonDbWorker _db;
        private static IDeanonSocNetworkWorker _sn;

        static void Main(string[] args)
        {
            Logger.AddTypeToUotput(MessageType.Error);
            Logger.AddTypeToUotput(MessageType.Verbose);

            _db = InitDb();
            _sn = InitVk();

            var deanon = DumpUser();
            //deanon.InitialDump(GetDepth()).Wait();
            deanon.CompleteRelations().Wait();
            Console.ReadLine();
        }

        static DumpingDepth GetDepth()
        {
            Console.WriteLine("Enter dumping depth for friends: ");
            int friend = int.Parse(Console.ReadLine());
            Console.WriteLine("Enter dumping depth for followers: ");
            int follower = int.Parse(Console.ReadLine());
            Console.WriteLine("Enter dumping depth for post: ");
            int post = int.Parse(Console.ReadLine());
            Console.WriteLine("Enter dumping depth for comments: ");
            int comment = int.Parse(Console.ReadLine());
            Console.WriteLine("Enter dumping depth for likes: ");
            int like = int.Parse(Console.ReadLine());


            return new DumpingDepth(
                new List<Depth>()
                {
                    new Depth( EnterType.Friend, friend ),
                    new Depth( EnterType.Follower, follower ),
                    new Depth( EnterType.Post, post ),
                    new Depth( EnterType.Comments, comment ),
                    new Depth( EnterType.Likes, like )
                });
        }

        static Deanon DumpUser()
        {
            Console.WriteLine("Enter user id to dump:");
            int userId = int.Parse(Console.ReadLine());
            return new Deanon(_db, _sn, userId);
        }

        static IDeanonDbWorker InitDb()
        {
            Console.WriteLine("Enter server address: ");
            String address = Console.ReadLine();
            Console.WriteLine("Enter server port: ");
            int port = int.Parse(Console.ReadLine());
            Console.WriteLine("Enter server login: ");
            String login = Console.ReadLine();
            Console.WriteLine("Enter server password: ");
            String password = Console.ReadLine();

            return new Neo4JWorker(address, port, login, password);
        }


        static IDeanonSocNetworkWorker InitVk()
        {
            Console.WriteLine("Enter VK token: ");
            return new VkWorker(new List<string>() { Console.ReadLine() });
        }


    }
}
