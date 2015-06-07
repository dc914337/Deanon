
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Deanon.db;
using Deanon.db.datamodels;
using Deanon.db.datamodels.classes.entities;
using Deanon.dumper.cache;
using Deanon.dumper.vk;
using Deanon.logger;
using VKSharp;
using VKSharp.Core.Entities;
using VKSharp.Core.Enums;
using VKSharp.Data.Parameters;
using VKSharp.Data.Request;
using MessageType = Deanon.logger.MessageType;


namespace Deanon.dumper
{
    class DeanonDumper
    {
        private readonly IDeanonDbWorker _neo4JWorker;
        private readonly IDeanonSocNetworkWorker _vkWorker;
        private readonly Cache _dbcache;

        public DeanonDumper(IDeanonDbWorker neo4JWorker, IDeanonSocNetworkWorker vkWorker, Cache cache)
        {
            _neo4JWorker = neo4JWorker;
            _neo4JWorker.Connect();
            _vkWorker = vkWorker;
            _dbcache = cache;
        }

        public async Task DumpUser(int userId, DumpingDepth depth)
        {
            Person user = await _vkWorker.GetPerson(userId);

            if (!_dbcache.CheckAddPersonId(userId))
                _neo4JWorker.AddPerson(user);

            await CollectPotentialFriendsRecursive(user, depth, new Dictionary<int, Person>());
        }

        private async Task CollectPotentialFriendsRecursive(Person user, DumpingDepth depth, Dictionary<int, Person> trace)
        {
            Logger.Out("Current user: {0}", MessageType.Verbose, user.Url);
            Logger.Out("Trace level: {0}", MessageType.Verbose, trace.Count);
            Logger.Out(depth.ToString(), MessageType.DebugDepth);

            if (trace.ContainsKey(user.Id))
            {
                Logger.Out("Hey! We've been here({0}).. ", MessageType.Debug, user.Url);
                return;
            }


            //add to trace
            trace.Add(user.Id, user);

            //add all friends that are not in neo4J
            if (depth.Enter(EnterType.Friend))
            {
                await DumpFriendsRecursive(user, depth, trace);
                depth.StepOut();
            }

            //add all followers that are not in neo4J
            if (depth.Enter(EnterType.Follower))
            {
                await DumpFollowersRecursive(user, depth, trace);
                depth.StepOut();
            }


            //dump recursive posts->comments->likes
            await CheckAndDumpPostsCommentsLikesRecursive(user, depth, trace);

            //remove from trace
            trace.Remove(user.Id);
        }


        private async Task CheckAndDumpPostsCommentsLikesRecursive(Person user, DumpingDepth depth, Dictionary<int, Person> trace)
        {
            try
            {
                if (!depth.Enter(EnterType.Post))
                {
                    Logger.Out("Hit bottom on posts", MessageType.Verbose, user.Url);
                    return;
                }

                var posts = await _vkWorker.GetAllPosts(user.Id);

                var postedPFriendsIds = posts.Select(a => a.FromId).Where(b => b != user.Id).Distinct().ToArray();
                await AddPotentialFriendsRecursive(user, EnterType.Post, await _vkWorker.GetPeople(postedPFriendsIds), depth, trace);
                depth.StepOut();


                if (!depth.Enter(EnterType.Comments))
                {
                    Logger.Out("Hit bottom on comments", MessageType.Verbose, user.Url);
                    return;
                }

                var comments = await _vkWorker.GetAllCommentsForPosts(user.Id, posts);

                var commentedPFriendsIds = comments.Select(a => a.FromId).Where(b => b != user.Id).Distinct().ToArray();
                await AddPotentialFriendsRecursive(user, EnterType.Comments, await _vkWorker.GetPeople(commentedPFriendsIds), depth, trace);
                depth.StepOut();


                if (!depth.Enter(EnterType.Likes))
                {
                    Logger.Out("Hit bottom on likes", MessageType.Verbose, user.Url);
                    return;
                }

                var likes = await _vkWorker.GetAllPeopleLiked(user.Id, posts, comments);
                var likedPFriendsIds = likes.Where(b => b != user.Id).Distinct().ToArray();
                await AddPotentialFriendsRecursive(user, EnterType.Likes, await _vkWorker.GetPeople(likedPFriendsIds), depth, trace);
                depth.StepOut();
            }
            catch (Exception ex)
            {
                Logger.Out("USER: {0} \r\n Message: {1}", MessageType.Error, user.Url, ex.Message);
            }
        }


        private async Task DumpFriendsRecursive(Person user, DumpingDepth depth, Dictionary<int, Person> trace)
        {
            Logger.Out("Dumping friends for user: {0}", MessageType.Verbose, user.Url);
            try
            {
                var friends = await _vkWorker.GetFriends(user);
                await AddPotentialFriendsRecursive(user, EnterType.Friend, friends, depth, trace);
            }
            catch (Exception ex)
            {
                Logger.Out("USER: {0} \r\n Message: {1}", MessageType.Error, user.Url, ex.Message);
            }


        }

        private async Task DumpFollowersRecursive(Person user, DumpingDepth depth, Dictionary<int, Person> trace)
        {
            Logger.Out("Dumping followers for user: {0}", MessageType.Verbose, user.Url);
            try
            {
                var followers = await _vkWorker.GetFollowers(user);
                await AddPotentialFriendsRecursive(user, EnterType.Follower, followers, depth, trace);
            }
            catch (Exception ex)
            {
                Logger.Out("USER: {0} \r\n Message: {1}", MessageType.Error, user.Url, ex.Message);
            }
        }


        private async Task AddPotentialFriendsRecursive(Person user, EnterType type, List<Person> potentialFriends, DumpingDepth depth, Dictionary<int, Person> trace)
        {
            foreach (var pFriend in potentialFriends)
            {
                if (!_dbcache.CheckAddPersonId(pFriend.Id))
                {
                    _neo4JWorker.AddPerson(pFriend);
                }
                _neo4JWorker.AddRelation(user, pFriend, type);
                await CollectPotentialFriendsRecursive(pFriend, depth, trace);
            }
        }

    }
}
