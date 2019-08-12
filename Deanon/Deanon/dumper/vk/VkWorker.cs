using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Deanon.db.datamodels;
using Deanon.db.datamodels.classes.entities;
using Deanon.logger;
using kasthack.vksharp;
using kasthack.vksharp.DataTypes.Entities;
using kasthack.vksharp.DataTypes.Enums;
using kasthack.vksharp.Implementation;

namespace Deanon.dumper.vk
{
    public class VkWorker : IDeanonSocNetworkWorker
    {
        private readonly TokenRepository _tokenRepo;
        private const int PostsPerTime = 2500;
        private const int LikesItemsPerTime = 25;
        private const int CommentsPostsPerTime = 25;
        private const int SleepMs = 333;

        public VkWorker(List<string> tokens)
        {
            this._tokenRepo = new TokenRepository();
            foreach (var token in tokens)
            {
                this._tokenRepo.AddToken(token);
            }
        }

        public async Task<Person> GetPerson(int userId)
        {
            await this.Sleep().ConfigureAwait(false);
            var vk = this.GetNewVkApi();
            return Mapper.MapPerson(
                (await vk.Users.Get(
                    userIds: new int[]
                    {
                        userId
                    }).ConfigureAwait(false))[0]);
        }

        public async Task<List<Person>> GetPeople(int[] userIds)
        {
            if (userIds.Length < 1)
            {
                return new List<Person>();
            }

            var vk = this.GetNewVkApi();
            await this.Sleep().ConfigureAwait(false);
            var users = await vk.Users.Get(userIds: userIds).ConfigureAwait(false);
            return users.Select(Mapper.MapPerson).ToList();
        }

        public async Task<List<Post>> GetAllPosts(int userId)
        {
            var firstBlock = (await this.GetBigWall(userId, 0).ConfigureAwait(false));

            var postsCount = firstBlock.Count;
            if (postsCount == 0)
            {
                return new List<Post>();
            }

            var posts = new List<Post>(postsCount);
            posts.AddRange(firstBlock.Items);
            for (var i = 0; i < postsCount / PostsPerTime; i++)
            {
                await this.Sleep().ConfigureAwait(false);
                posts.AddRange((await this.GetBigWall(userId, (i + 1) * PostsPerTime).ConfigureAwait(false)).Items);
            }

            return posts;
        }

        public async Task<List<Comment>> GetAllCommentsForPosts(int userId, List<Post> posts)
        {
            var comments = new List<Comment>();
            var postIdsWithComments = posts.Where(a => a.Comments.Count > 0).Select(a => (int)a.Id).ToArray();

            var pointer = 0;

            if (postIdsWithComments.Length == 0)
            {
                return comments;
            }

            var postIdsDose = new List<int>();
            foreach (var postId in postIdsWithComments)
            {
                postIdsDose.Add(postId);
                pointer++;
                if (pointer == CommentsPostsPerTime)
                {
                    pointer = 0;
                    await this.Sleep().ConfigureAwait(false);
                    var manyCommentsEntity = await this.GetManyComments(userId, postIdsDose).ConfigureAwait(false);
                    comments.AddRange(manyCommentsEntity.Items);
                    postIdsDose.Clear();
                }
            }
            if (postIdsDose.Any())
            {
                await this.Sleep().ConfigureAwait(false);
                comments.AddRange((await this.GetManyComments(userId, postIdsDose).ConfigureAwait(false)).Items);
            }

            return comments;
        }

        public async Task<List<int>> GetAllPeopleLiked(int userId, List<Post> posts, List<Comment> comments)
        {
            var likedPosts = posts.Where(a => a.Likes.Count != 0).Select(a => (int)a.Id).ToList();
            var likedComments = comments.Where(a => a.Likes.Count != 0).Select(a => (int)a.Id).ToList();
            var peopleLiked = await this.GetAllLikes(userId, true, likedPosts).ConfigureAwait(false);
            peopleLiked.AddRange(await this.GetAllLikes(userId, false, likedComments).ConfigureAwait(false));
            return peopleLiked.Distinct().ToList();
        }

        public async Task<List<Person>> GetFriends(Person user)
        {
            if (user.Deleted)
            {
                Logger.Out("Person {0} is deleted(or banned). Can't get friends", logger.MessageType.Debug, user.Id);
                return new List<Person>();
            }

            var vk = this.GetNewVkApi();
            await this.Sleep().ConfigureAwait(false);
            return (await vk.Friends.Get(userId: user.Id, fields: UserFields.Anything, count: 1000000).ConfigureAwait(false)).Items.Select(Mapper.MapPerson).ToList();
        }

        public async Task<List<Person>> GetFollowers(Person user)
        {
            var vk = this.GetNewVkApi();
            await this.Sleep().ConfigureAwait(false);
            return (await vk.Users.GetFollowers(userId: user.Id, fields: UserFields.Anything, count: 1000).ConfigureAwait(false)).Items.Select(Mapper.MapPerson).ToList();//fix
        }

        private async Task<EntityList<Post>> GetBigWall(int ownerId, int offset)
        {
            var vk = this.GetNewVkApi();
            var req = new Request<EntityList<Post>>()
            {
                MethodName = "execute.wallGet25r",
                Token = vk.CurrentToken,
                Parameters = new Dictionary<string, string>(){
                    { "offset", offset.ToString()},
                    { "owner_id", ownerId.ToString()}
            }
            };
            await this.Sleep().ConfigureAwait(false);
            return (await vk.Executor.ExecAsync(req).ConfigureAwait(false)).Response;
        }

        private async Task<EntityList<int>> GetManyLikes(int ownerId, List<int> itemIds, string type)
        {
            await this.Sleep().ConfigureAwait(false);
            var vk = this.GetNewVkApi();
            var parameters = new Dictionary<string, string>(){
                     {"owner_id", ownerId.ToString()},
                     {"offset", "0"},
                     {"type",type}
            };

            for (var i = 0; i < LikesItemsPerTime && i < itemIds.Count; i++)
            {
                parameters.Add("item_id" + (i + 1), itemIds[i].ToString());
            }

            var req = new Request<EntityList<int>>()
            {
                MethodName = "execute.getLikes25r",
                Token = vk.CurrentToken,
                Parameters = parameters
            };
            return (await vk.Executor.ExecAsync(req).ConfigureAwait(false)).Response;
        }

        //post - true(post)/false(comment)
        private async Task<List<int>> GetAllLikes(int ownerId, bool post, List<int> ids)
        {
            var likers = new List<int>();

            var pointer = 0;

            if (!ids.Any())
            {
                return likers;
            }

            var itemIdsDose = new List<int>();
            foreach (var itemId in ids)
            {
                itemIdsDose.Add(itemId);
                pointer++;
                if (pointer == LikesItemsPerTime)
                {
                    pointer = 0;
                    likers.AddRange((await this.GetManyLikes(ownerId, itemIdsDose, post ? "post" : "comment").ConfigureAwait(false)).Items);
                    itemIdsDose.Clear();
                }
            }
            if (itemIdsDose.Any())
            {
                likers.AddRange((await this.GetManyLikes(ownerId, itemIdsDose, post ? "post" : "comment").ConfigureAwait(false)).Items);
            }

            return likers;
        }

        //works only with 25 posts!
        private async Task<EntityList<Comment>> GetManyComments(int ownerId, List<int> postIds)
        {
            var vk = this.GetNewVkApi();
            var parameters = new Dictionary<string, string>(){
                     {"owner_id", ownerId.ToString()},
                     {"offset", "0"}
            };

            for (var i = 0; i < CommentsPostsPerTime && i < postIds.Count; i++)
            {
                parameters.Add("post_id" + (i + 1), postIds[i].ToString());
            }

            var req = new Request<EntityList<Comment>>()
            {
                MethodName = "execute.get25Comments",
                Token = vk.CurrentToken,
                Parameters = parameters
            };
            await this.Sleep().ConfigureAwait(false);
            var result = (await vk.Executor.ExecAsync(req).ConfigureAwait(false)).Response;
            return result;
        }

        private Api GetNewVkApi()
        {
            var api = new Api();
            api.AddToken(this._tokenRepo.GetToken());
            return api;
        }

        private async Task Sleep() => await Task.Delay(SleepMs).ConfigureAwait(false);
    }
}
