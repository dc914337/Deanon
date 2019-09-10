
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Deanon.db;
using Deanon.db.datamodels.classes.entities;
using Deanon.dumper.vk;
using Deanon.logger;
using MessageType = Deanon.logger.MessageType;

namespace Deanon.dumper
{
    public class VkDumper
    {
        private readonly DbWorker _dbWorker;
        private readonly VkWorker _vkWorker;

        public VkDumper(DbWorker dbWorker, VkWorker vkWorker)
        {
            this._dbWorker = dbWorker;
            this._dbWorker.Connect();
            this._vkWorker = vkWorker;
        }

        public async Task DumpUser(int userId, DumpingDepth depth)
        {
            var user = await this._vkWorker.GetPerson(userId).ConfigureAwait(false);
            this._dbWorker.AddPerson(user);

            await this.CollectPotentialFriendsRecursive(user, depth, new Dictionary<int, Person>()).ConfigureAwait(false);
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
                await this.DumpFriendsRecursive(user, depth, trace).ConfigureAwait(false);
                depth.StepOut();
            }

            //add all followers that are not in db
            if (depth.Enter(EnterType.Follower))
            {
                await this.DumpFollowersRecursive(user, depth, trace).ConfigureAwait(false);
                depth.StepOut();
            }

            //dump recursive posts->comments->likes
            await this.CheckAndDumpPostsCommentsLikesRecursive(user, depth, trace).ConfigureAwait(false);

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

                var posts = await this._vkWorker.GetAllPosts(user.Id).ConfigureAwait(false);

                var postedPFriendsIds = posts.Select(a => a.FromId).Where(b => b != user.Id).Distinct().ToArray();
                await this.AddPotentialFriendsRecursive(user, EnterType.Post, await this._vkWorker.GetPeople(postedPFriendsIds).ConfigureAwait(false), depth, trace).ConfigureAwait(false);
                depth.StepOut();

                if (!depth.Enter(EnterType.Comments))
                {
                    Logger.Out("Hit bottom on comments", MessageType.Verbose, user.Url);
                    return;
                }

                var comments = await this._vkWorker.GetAllCommentsForPosts(user.Id, posts).ConfigureAwait(false);

                var commentedPFriendsIds = comments.Select(a => a.FromId).Where(b => b != user.Id).Distinct().ToArray();
                await this.AddPotentialFriendsRecursive(user, EnterType.Comments, await this._vkWorker.GetPeople(commentedPFriendsIds).ConfigureAwait(false), depth, trace).ConfigureAwait(false);
                depth.StepOut();

                if (!depth.Enter(EnterType.Likes))
                {
                    Logger.Out("Hit bottom on likes", MessageType.Verbose, user.Url);
                    return;
                }

                var likes = await this._vkWorker.GetAllPeopleLiked(user.Id, posts, comments).ConfigureAwait(false);
                var likedPFriendsIds = likes.Where(b => b != user.Id).Distinct().ToArray();
                await this.AddPotentialFriendsRecursive(user, EnterType.Likes, await this._vkWorker.GetPeople(likedPFriendsIds).ConfigureAwait(false), depth, trace).ConfigureAwait(false);
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
                var friends = await this._vkWorker.GetFriends(user).ConfigureAwait(false);
                await this.AddPotentialFriendsRecursive(user, EnterType.Friend, friends, depth, trace).ConfigureAwait(false);
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
                var followers = await this._vkWorker.GetFollowers(user).ConfigureAwait(false);
                await this.AddPotentialFriendsRecursive(user, EnterType.Follower, followers, depth, trace).ConfigureAwait(false);
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
                this._dbWorker.AddPotentialFriend(user, pFriend, type);
                await this.CollectPotentialFriendsRecursive(pFriend, depth, trace).ConfigureAwait(false);
            }
        }
    }
}
