
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Deanon.db;
using Deanon.db.datamodels.classes.entities;
using Deanon.dumper;
using Deanon.dumper.vk;
using Deanon.logger;
using Newtonsoft.Json;

namespace Deanon
{

    public static class Program
    {
        private static Configuration.Config config;

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

        private static async Task Main()
        {
            config = JsonConvert.DeserializeObject<Configuration.Config>(File.ReadAllText("config.json"));
            ConfigureLogger();

            InitDBAndVK();

            var _watches = Stopwatch.StartNew();
            var deanon = new Deanon(_db, _sn, config.Exec.UserId, config.Exec.ContinueFromSavePoint);

            if (config.Stages.Small)
            {
                await SmallDump(_watches, deanon).ConfigureAwait(false);
            }

            if (config.Stages.Big)
            {
                var depth = GetDepth();
                await BigDump(deanon, depth).ConfigureAwait(false);
            }

            for (var i = 0; i < config.Exec.Expansions; i++)
            {
                await ExpansionDump(deanon).ConfigureAwait(false);
            }
        }

        private static async Task ExpansionDump(Deanon deanon)
        {
            var _watches = Stopwatch.StartNew();
            await deanon.ExpansionDump(
                new DumpingDepth(
                    new List<Depth>()
                    {
                        new Depth( EnterType.Friend, 1 ),
                        new Depth( EnterType.Follower, 0 ),
                        new Depth( EnterType.Post, 0 ),
                        new Depth( EnterType.Comments, 0 ),
                        new Depth( EnterType.Likes, 0 )
                    })).ConfigureAwait(false); //10
            await CompleteRelations(deanon).ConfigureAwait(false);
            var hiddenFriendsExpansion = deanon.GetHiddenFriends(); //12
            OutputUsers(hiddenFriendsExpansion); //12
            RestartWatchesAndShowTime(_watches, "expansion dump and complete relations");
        }

        private static async Task BigDump(Deanon deanon, DumpingDepth depth)
        {
            var _watches = Stopwatch.StartNew();
            await deanon.InitialDump(depth).ConfigureAwait(false); //7
            await CompleteRelations(deanon).ConfigureAwait(false);
            var hiddenFriendsBig = deanon.GetHiddenFriends(); //8
            OutputUsers(hiddenFriendsBig); //9
            RestartWatchesAndShowTime(_watches, "big initial dump and complete relations");
        }

        private static async Task CompleteRelations(Deanon deanon) => await deanon.CompleteRelations(config.Exec.CompleteRelations).ConfigureAwait(false);

        private static async Task SmallDump(Stopwatch _watches, Deanon deanon)
        {
            _watches = RestartWatchesAndShowTime(_watches, "startup");

            await deanon.InitialDump(InitialDepth).ConfigureAwait(false); //2
            await CompleteRelations(deanon).ConfigureAwait(false);
            var hiddenFriendsSmall = deanon.GetHiddenFriends(); //4
            OutputUsers(hiddenFriendsSmall); //4

            RestartWatchesAndShowTime(_watches, "small dump initial user and complete relations");
        }

        private static void InitDBAndVK()
        {
            _db = new Neo4JWorker(config.DB.Address, config.DB.Port, config.DB.User, config.DB.Password);
            _sn = new VkWorker(new List<string>() { config.VK.Token });
        }

        private static void ConfigureLogger()
        {
            Logger.AddTypeToOutput(MessageType.Error);
            Logger.AddTypeToOutput(MessageType.Verbose);
            Logger.AddTypeToOutput(MessageType.DebugCache);
            Logger.AddTypeToOutput(MessageType.Time);
            Logger.AddTypeToOutput(MessageType.Debug);
        }

        private static Stopwatch RestartWatchesAndShowTime(Stopwatch sw, string operationName)
        {
            sw.Stop();
            Logger.Out("Operation '{0}' completed in {1} seconds!", MessageType.Time, operationName, sw.ElapsedMilliseconds / 1000);
            return Stopwatch.StartNew();
        }

        private static void OutputUsers(Person[] people)
        {
            foreach (var person in people)
            {
                Logger.Out(person.Url, MessageType.Verbose);
            }
        }

        private static DumpingDepth GetDepth()
        {
            var cfg = config.Depth;
            return new DumpingDepth(
                new List<Depth>()
                {
                    new Depth( EnterType.Friend, cfg.Friends ),
                    new Depth( EnterType.Follower, cfg.Followers ),
                    new Depth( EnterType.Post, cfg.Post ),
                    new Depth( EnterType.Comments, cfg.Comments ),
                    new Depth( EnterType.Likes, cfg.Likes)
                });
        }
    }
}
