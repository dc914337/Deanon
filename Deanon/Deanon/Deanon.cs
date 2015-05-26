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
using Deanon.dumper.vk;
using Deanon.logger;

namespace Deanon
{
    class Deanon
    {
        private DeanonDumper _dumper;
        private IDeanonDbWorker _dbWorker;
        private IDeanonSocNetworkWorker _snWorker;
        private int _userId;
        private DbGraphAnalyzer _analyzer;
        public Deanon(IDeanonDbWorker dbWorker, IDeanonSocNetworkWorker snWorker, int userId)
        {
            this._snWorker = snWorker;
            _dbWorker = dbWorker;
            _dumper = new DeanonDumper(dbWorker, snWorker);
            _analyzer = new DbGraphAnalyzer(dbWorker);
            this._userId = userId;
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
                _dumper.DumpUser(person.Id, depth).Wait();
            }

        }

        public async Task CompleteRelations()
        {
            HashSet<long> userIds = new HashSet<long>(_dbWorker.GetAllUsersIds());
            //select people who have no out relations
            //var people = _analyzer.GetPeopleWithoutOutRelations();
            //get all people
            var people = _analyzer.GetAllPeople();
            int count = 0;
            foreach (var person in people)
            {
                count++;
                Logger.Out("Completing: {0}. {1}/{2} done!", MessageType.Verbose, person.Id, count, people.Count());
                var friends=new List<Person>();
                try
                {
                    friends = await _snWorker.GetFriends(person);
                }
                catch (Exception ex)
                {
                    Logger.Out("Error getting friends for user {0}. Message: {1}", MessageType.Error, person.Id, ex.Message);
                }
                foreach (var friend in friends)
                {
                    if (userIds.Contains(friend.Id))
                    {
                        try
                        {
                            _dbWorker.AddPotentialFriend(person, friend, EnterType.Friend);
                            Logger.Out("Added relation: {0} --> {1}", MessageType.Verbose, person.Id, friend.Id);
                        }
                        catch (Exception ex)
                        {
                            Logger.Out("Error adding relation: {0} --> {1}. Message: {2}", MessageType.Error, person.Id, friend.Id, ex.Message);
                        }


                    }
                }
            }

        }
    }
}
