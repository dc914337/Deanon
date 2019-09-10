using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Deanon.analyzer;
using Deanon.db;
using Deanon.db.datamodels.classes.entities;
using Deanon.dumper;
using Deanon.dumper.cache;
using Deanon.dumper.vk;
using Deanon.logger;

namespace Deanon
{
    public class Deanon
    {
        private readonly DeanonDumper _dumper;
        private readonly IDeanonDbWorker _dbWorker;
        private readonly IDeanonSocNetworkWorker _snWorker;
        private readonly int _userId;
        private readonly Cache _cache;

        private readonly DbGraphAnalyzer _analyzer;

        public Deanon(IDeanonDbWorker dbWorker, IDeanonSocNetworkWorker snWorker, int userId, bool continueDump)
        {
            this._userId = userId;
            this._snWorker = snWorker;
            this._dbWorker = dbWorker;
            this._cache = new Cache();
            this._dumper = new DeanonDumper(dbWorker, snWorker, this._cache);
            this._analyzer = new DbGraphAnalyzer(dbWorker);
            if (continueDump)
            {
                this.WarmUpCache();
            }
            else
            {
                //clear database
                this._dbWorker.ClearDatabase();//0             
            }
        }

        private void WarmUpCache()
        {
            Logger.Out("Warming up cache(people id's)", MessageType.DebugCache);
            this._cache.AddManyIds(this._dbWorker.GetAllUsersIds());
            Logger.Out("Cache is hot!", MessageType.DebugCache);
        }

        public async Task InitialDump(DumpingDepth depth)
        {
            var sw = new Stopwatch();
            sw.Start();
            await this._dumper.DumpUser(this._userId, depth).ConfigureAwait(false);
            sw.Stop();
            Logger.Out("Done in {0} seconds", MessageType.Verbose, sw.ElapsedMilliseconds / 1000);
        }

        public async Task ExpansionDump(DumpingDepth depth)
        {
            var people = this._analyzer.GetPeopleInCycles();
            var maxCount = people.Length;
            var counter = 0;
            foreach (var person in people)
            {
                Logger.Out("Current expansion is {0}%", MessageType.Verbose, (double)100 / maxCount * (++counter));
                #region Timer start
                var sw = new Stopwatch();
                sw.Start();
                #endregion
                await this._dumper.DumpUser(person.Id, depth).ConfigureAwait(false);
                sw.Stop(); Logger.Out("Expansion node added in {0} seconds", MessageType.Verbose, sw.Elapsed.TotalSeconds);
            }
        }

        public async Task CompleteRelations(bool full)
        {
            var userIds = new HashSet<int>(this._dbWorker.GetAllUsersIds());
            //select people who have no out relations
            //var people = _analyzer.GetPeopleWithoutOutRelationsAndNotDeleted();
            //get all people
            var people = this._analyzer.GetPeopleWithoutOutRelations();
            var count = 0;
            foreach (var person in people)
            {
                count++;
                Logger.Out("Completing: {0}. {1}/{2} done!", MessageType.Verbose, person.Id, count, people.Length);
                var friends = new List<Person>();
                try
                {
                    friends = await this._snWorker.GetFriends(person).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Out("Error getting friends for user {0}. Message: {1}", MessageType.Error, person.Id, ex.Message);
                }

                if (full)
                {
                    this.AddAllRelations(friends, userIds, person);
                }
                else
                {
                    var mainUser = friends.FirstOrDefault(a => a.Id == this._userId);
                    if (mainUser != null)
                    {
                        try
                        {
                            this._dbWorker.AddRelation(person, mainUser, EnterType.Friend);
                        }
                        catch (Exception ex)
                        {
                            Logger.Out("Error adding relation: {0} --> {1}. Message: {2}", MessageType.Error, person.Id, mainUser.Id, ex.Message);
                        }
                    }
                }
            }
        }

        private void AddAllRelations(List<Person> friends, HashSet<int> userIds, Person person)
        {
            foreach (var friend in friends)
            {
                if (userIds.Contains(friend.Id))
                {
                    try
                    {
                        this._dbWorker.AddRelation(person, friend, EnterType.Friend);
                    }
                    catch (Exception ex)
                    {
                        Logger.Out("Error adding relation: {0} --> {1}. Message: {2}", MessageType.Error, person.Id, friend.Id, ex.Message);
                    }
                }
            }
        }

        public Person[] GetHiddenFriends() => this._dbWorker.GetHiddenFriendsOfUser(this._userId);
    }
}
