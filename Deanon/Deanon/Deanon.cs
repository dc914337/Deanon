using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
    class Deanon
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
            _dbWorker = dbWorker;
            _cache = new Cache();
            _dumper = new DeanonDumper(dbWorker, snWorker, _cache);
            _analyzer = new DbGraphAnalyzer(dbWorker);
            if (continueDump)
            {
                WarmUpCache();
            }
            else
            {
                //clear database
                _dbWorker.ClearDatabase();//0             
            }
        }

        private void WarmUpCache()
        {
            Logger.Out("Warming up cache(people id's)", MessageType.DebugCache);
            _cache.AddManyIds(_dbWorker.GetAllUsersIds());
            Logger.Out("Cache is hot!", MessageType.DebugCache);
        }


        public async Task InitialDump(DumpingDepth depth)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            await _dumper.DumpUser(_userId, depth);
            sw.Stop();
            Logger.Out("Done in {0} seconds", MessageType.Verbose, sw.ElapsedMilliseconds / 1000);
        }


        public void ExpansionDump(DumpingDepth depth)
        {
            var people = _analyzer.GetPeopleInCycles();
            int maxCount = people.Count();
            int counter = 0;
            foreach (var person in people)
            {
                Logger.Out("Current expansion is {0}%", MessageType.Verbose, (double)100 / maxCount * (++counter));
                #region Timer start
                Stopwatch sw = new Stopwatch();
                sw.Start();
                #endregion
                _dumper.DumpUser(person.Id, depth).Wait();
                sw.Stop(); Logger.Out("Expansion node added in {0} seconds", MessageType.Verbose, sw.Elapsed.TotalSeconds);
            }

        }

        public async Task CompleteRelations(bool full)
        {
            HashSet<int> userIds = new HashSet<int>(_dbWorker.GetAllUsersIds());
            //select people who have no out relations
            //var people = _analyzer.GetPeopleWithoutOutRelationsAndNotDeleted();
            //get all people
            var people = _analyzer.GetPeopleWithoutOutRelations();
            int count = 0;
            foreach (var person in people)
            {
                count++;
                Logger.Out("Completing: {0}. {1}/{2} done!", MessageType.Verbose, person.Id, count, people.Count());
                var friends = new List<Person>();
                try
                {
                    friends = await _snWorker.GetFriends(person);
                }
                catch (Exception ex)
                {
                    Logger.Out("Error getting friends for user {0}. Message: {1}", MessageType.Error, person.Id, ex.Message);
                }

                if (full)
                {
                    AddAllRelations(friends, userIds, person);
                }
                else
                {
                    Person mainUser = friends.FirstOrDefault(a => a.Id == _userId);
                    if (mainUser != null)
                    {
                        try
                        {
                            _dbWorker.AddRelation(person, mainUser, EnterType.Friend);
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
                        _dbWorker.AddRelation(person, friend, EnterType.Friend);
                    }
                    catch (Exception ex)
                    {
                        Logger.Out("Error adding relation: {0} --> {1}. Message: {2}", MessageType.Error, person.Id, friend.Id, ex.Message);
                    }
                }
            }
        }




        public Person[] GetHiddenFriends()
        {
            return _dbWorker.GetHiddenFriendsOfUser(_userId);
        }
    }
}
