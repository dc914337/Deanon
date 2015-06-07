
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

        private static readonly DumpingDepth InitialDepth = new DumpingDepth(
            new List<Depth>()
            {
                new Depth(EnterType.Friend, 1 ),
                new Depth(EnterType.Follower, 1 ),
                new Depth(EnterType.Post, 1 ),
                new Depth(EnterType.Comments, 1 ),
                new Depth(EnterType.Likes, 1 )
            });

        static void Main(string[] args)
        {
            ConfigureLogger();

            InitDBAndVK();



            var _watches = StartOperation();
            var deanon = DumpUser();


            if (!SkipRequest("small dump"))
            {
                SmallDump(_watches, deanon);
                if (ExitRequest())
                    return;
            }

            if (!SkipRequest("big dump"))
            {
                var depth = GetDepth();
                BigDump(deanon, depth);
            }

            while (true)
            {
                if (ExitRequest())
                    return;
                ExpansionDump(deanon);
            }
        }

        private static void ExpansionDump(Deanon deanon)
        {
            Stopwatch _watches;
            _watches = StartOperation();
            deanon.ExpansionDump(
                new DumpingDepth(
                    new List<Depth>()
                    {
                        new Depth( EnterType.Friend, 1 ),
                        new Depth( EnterType.Follower, 0 ),
                        new Depth( EnterType.Post, 0 ),
                        new Depth( EnterType.Comments, 0 ),
                        new Depth( EnterType.Likes, 0 )
                    })); //10
            CompleteRelations(deanon);
            Person[] hiddenFriendsExpansion = deanon.GetHiddenFriends(); //12
            OutputUsers(hiddenFriendsExpansion); //12
            RestartWatchesAndShowTime(_watches, "expansion dump and complete relations");
        }

        private static void BigDump(Deanon deanon, DumpingDepth depth)
        {
            Stopwatch _watches;
            _watches = StartOperation();
            deanon.InitialDump(depth).Wait(); //7
            CompleteRelations(deanon);
            Person[] hiddenFriendsBig = deanon.GetHiddenFriends(); //8
            OutputUsers(hiddenFriendsBig); //9
            RestartWatchesAndShowTime(_watches, "big initial dump and complete relations");
        }


        private static void CompleteRelations(Deanon deanon)
        {
            Logger.Out("Do you want to complete all relations?(y/n)", MessageType.Verbose);
            bool full = Console.ReadLine().ToLower().Trim() == "y";
            deanon.CompleteRelations(full).Wait();
        }



        private static void SmallDump(Stopwatch _watches, Deanon deanon)
        {
            _watches = RestartWatchesAndShowTime(_watches, "startup");

            deanon.InitialDump(InitialDepth).Wait(); //2
            CompleteRelations(deanon);
            Person[] hiddenFriendsSmall = deanon.GetHiddenFriends(); //4
            OutputUsers(hiddenFriendsSmall); //4

            RestartWatchesAndShowTime(_watches, "small dump initial user and complete relations");
        }

        private static void InitDBAndVK()
        {
            _db = InitDb();
            _sn = InitVk();
        }

        static void ConfigureLogger()
        {
            Logger.AddTypeToUotput(MessageType.Error);
            Logger.AddTypeToUotput(MessageType.Verbose);
            Logger.AddTypeToUotput(MessageType.DebugCache);
            Logger.AddTypeToUotput(MessageType.Time);
            Logger.AddTypeToUotput(MessageType.Debug);
        }


        static Stopwatch StartOperation()
        {
            var sw = new Stopwatch();
            sw.Start();
            return sw;
        }

        static Stopwatch RestartWatchesAndShowTime(Stopwatch sw, String operationName)
        {
            sw.Stop();
            Logger.Out("Operation '{0}' completed in {1} seconds!", MessageType.Time, operationName, sw.ElapsedMilliseconds / 1000);
            return StartOperation();
        }


        static void OutputUsers(Person[] people)
        {
            foreach (var person in people)
            {
                Logger.Out(person.Url, MessageType.Verbose);
            }
        }

        static bool ExitRequest()
        {
            Logger.Out("Do you want to continue search? Y/N", MessageType.Verbose);
            return Console.ReadLine().ToLower() != "y";
        }

        static bool SkipRequest(String stageName)
        {
            Logger.Out("Do you want to skip '{0}' stage? Y/N", MessageType.Verbose, stageName);
            return Console.ReadLine().ToLower() == "y";
        }

        static DumpingDepth GetDepth()
        {
            Logger.Out("--You need to input dumping depth here--", MessageType.Verbose);
            Logger.Out("Enter dumping depth for friends: ", MessageType.Verbose);
            int friend = int.Parse(Console.ReadLine());
            Logger.Out("Enter dumping depth for followers: ", MessageType.Verbose);
            int follower = int.Parse(Console.ReadLine());
            Logger.Out("Enter dumping depth for post: ", MessageType.Verbose);
            int post = int.Parse(Console.ReadLine());
            Logger.Out("Enter dumping depth for comments: ", MessageType.Verbose);
            int comment = int.Parse(Console.ReadLine());
            Logger.Out("Enter dumping depth for likes: ", MessageType.Verbose);
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

            Console.WriteLine("Refresh database(Y/N):");
            bool continueDump = Console.ReadLine().ToLower() != "y";
            return new Deanon(_db, _sn, userId, continueDump);
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
