
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Deanon.db;
using Deanon.db.datamodels;
using Deanon.db.datamodels.classes.entities;
using Deanon.logger;
using Deanon.vk;
using VKSharp;
using VKSharp.Core.Entities;
using VKSharp.Core.Enums;
using VKSharp.Data.Parameters;
using VKSharp.Data.Request;
using MessageType = Deanon.logger.MessageType;

//using VKSharp;
//using VKSharp.Data.Parameters;

namespace Deanon.dumper
{
    class VkDumper
    {

        private DbWorker _dbWorker;
        private VkWorker _vkWorker;


        public VkDumper(DbWorker dbWorker, VkWorker vkWorker)
        {
            _dbWorker = dbWorker;
            _dbWorker.Connect();
            _vkWorker = vkWorker;
        }


        public async Task DumpUser(int userId, DumpingDepth depth)
        {
            var user = await _vkWorker.GetPerson(userId);
            _dbWorker.AddPerson(user);

            await CollectPotentialFriendsRecursive(user, depth, new Dictionary<int, Person>());
        }

        private async Task CollectPotentialFriendsRecursive(Person user, DumpingDepth depth, Dictionary<int, Person> trace)
        {
            Logger.Out("Current user: {0}", MessageType.Verbose, user.Url);
            Logger.Out("Trace level: {0}", MessageType.Verbose, trace.Count);
            Logger.Out(depth.ToString(), MessageType.Debug);

            if (trace.ContainsKey(user.Id))
            {
                Logger.Out("Hey! We've been here({0}).. ", MessageType.Debug, user.Url);
                return;
            }


            //add to trace
            trace.Add(user.Id, user);

            //add all friends that are not in db
            if (depth.Enter(EnterType.Friend))
            {
                await DumpFriendsRecursive(user, depth, trace);
                depth.StepOut();
            }

            //add all followers that are not in db
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
                _dbWorker.AddPotentialFriend(user, pFriend, type);
                await CollectPotentialFriendsRecursive(pFriend, depth, trace);
            }
        }

    }
}
