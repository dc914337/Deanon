using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Deanon.db.datamodels;
using Deanon.db.datamodels.classes.entities;
using VKSharp;
using VKSharp.Core.Entities;
using VKSharp.Data.Executors;
using VKSharp.Data.Parameters;
using VKSharp.Data.Request;
using VKSharp.Helpers;
using VKSharp.Helpers.Exceptions;
using Newtonsoft.Json;

namespace Deanon.dumper.vk
{
    class VkWorker : IDeanonSocNetworkWorker
    {
        private readonly VkTokenRepository _tokenRepo;
        private const int PostsPerTime = 2500;
        private const int LikesItemsPerTime = 25;
        private const int CommentsPostsPerTime = 25;
        private const int SleepMs = 333;

        public VkWorker(List<String> tokens)
        {
            Sleep();
            _tokenRepo = new VkTokenRepository();
            foreach (var token in tokens)
            {
                _tokenRepo.AddToken(token);
            }
        }

        public async Task<Person> GetPerson(int userId)
        {
            Sleep();
            var vk = GetNewVkApi();
            return Mapper.MapPerson(
                (await vk.Users.Get(
                    userIds: new int[]
                    {
                        userId
                    }))[0]);
        }


        public async Task<List<Person>> GetPeople(int[] userIds)
        {
            var vk = GetNewVkApi();
            Sleep();
            return (await vk.Users.Get(userIds: userIds)).Select(Mapper.MapPerson).ToList();
        }


        public async Task<List<Post>> GetAllPosts(int userId)
        {
            var firstBlock = (await GetBigWall(userId, 0));

            int postsCount = firstBlock.Count;
            if (postsCount == 0)
                return new List<Post>();

            List<Post> posts = new List<Post>(postsCount);
            posts.AddRange(firstBlock.Items);
            for (int i = 0; i < postsCount / PostsPerTime; i++)
            {
                Sleep();
                posts.AddRange((await GetBigWall(userId, (i + 1) * PostsPerTime)).Items);
            }

            return posts;
        }

        public async Task<List<Comment>> GetAllCommentsForPosts(int userId, List<Post> posts)
        {
            List<Comment> comments = new List<Comment>();
            int[] postIdsWithComments = posts.Where(a => a.Comments.Count > 0).Select(a => (int)a.Id).ToArray();

            int pointer = 0;

            if (postIdsWithComments.Length == 0)
                return comments;


            List<int> postIdsDose = new List<int>();
            foreach (var postId in postIdsWithComments)
            {
                postIdsDose.Add(postId);
                pointer++;
                if (pointer == CommentsPostsPerTime)
                {
                    pointer = 0;
                    Sleep();
                    var manyCommentsEntity = await GetManyComments(userId, postIdsDose);
                    comments.AddRange(manyCommentsEntity.Items);
                    postIdsDose.Clear();
                }
            }
            if (postIdsDose.Any())
            {
                Sleep();
                comments.AddRange((await GetManyComments(userId, postIdsDose)).Items);
            }


            return comments;
        }

        public async Task<List<int>> GetAllPeopleLiked(int userId, List<Post> posts, List<Comment> comments)
        {
            List<int> likedPosts = posts.Where(a => a.Likes.Count != 0).Select(a => (int)a.Id).ToList();
            List<int> likedComments = comments.Where(a => a.Likes.Count != 0).Select(a => (int)a.Id).ToList();
            List<int> peopleLiked = await GetAllLikes(userId, true, likedPosts);
            peopleLiked.AddRange(await GetAllLikes(userId, false, likedComments));
            return peopleLiked.Distinct().ToList();
        }

        public async Task<List<Person>> GetFriends(Person user)
        {
            var vk = GetNewVkApi();
            Sleep();
            return (await vk.Friends.Get(userId: user.Id, fields: UserFields.Anything, count: 1000000)).Items.Select(Mapper.MapPerson).ToList();
        }


        public async Task<List<Person>> GetFollowers(Person user)
        {
            var vk = GetNewVkApi();
            Sleep();
            return (await vk.Users.GetFollowers(userId: user.Id, fields: UserFields.Anything, count: 1000)).Items.Select(Mapper.MapPerson).ToList();//fix
        }

        private async Task<EntityList<Post>> GetBigWall(int ownerId, int offset)
        {
            var vk = GetNewVkApi();
            var req = new VKRequest<EntityList<Post>>()
            {
                MethodName = "execute.wallGet25r",
                Token = vk.CurrentToken,
                Parameters = new Dictionary<string, string>(){
                    { "offset", offset.ToString()},
                    { "owner_id", ownerId.ToString()}
            }
            };
            Sleep();
            return (await vk.Executor.ExecAsync(req)).Response;
        }



        private async Task<EntityList<int>> GetManyLikes(int ownerId, List<int> itemIds, String type)
        {
            Sleep();
            var vk = GetNewVkApi();
            Dictionary<string, string> parameters = new Dictionary<string, string>(){
                     {"owner_id", ownerId.ToString()},
                     {"offset", "0"},
                     {"type",type}
            };

            for (int i = 0; i < LikesItemsPerTime && i < itemIds.Count; i++)
            {
                parameters.Add("item_id" + (i + 1), itemIds[i].ToString());
            }

            var req = new VKRequest<EntityList<int>>()
            {
                MethodName = "execute.getLikes25r",
                Token = vk.CurrentToken,
                Parameters = parameters
            };
            return (await vk.Executor.ExecAsync(req)).Response;
        }


        //post - true(post)/false(comment)
        private async Task<List<int>> GetAllLikes(int ownerId, bool post, List<int> ids)
        {
            List<int> likers = new List<int>();

            int pointer = 0;

            if (!ids.Any())
                return likers;

            List<int> itemIdsDose = new List<int>();
            foreach (var itemId in ids)
            {
                itemIdsDose.Add(itemId);
                pointer++;
                if (pointer == LikesItemsPerTime)
                {
                    pointer = 0;
                    likers.AddRange((await GetManyLikes(ownerId, itemIdsDose, post ? "post" : "comment")).Items);
                    itemIdsDose.Clear();
                }
            }
            if (itemIdsDose.Any())
            {
                likers.AddRange((await GetManyLikes(ownerId, itemIdsDose, post ? "post" : "comment")).Items);
            }

            return likers;
        }


        //works only with 25 posts!
        private async Task<EntityList<Comment>> GetManyComments(int ownerId, List<int> postIds)
        {
            var vk = GetNewVkApi();
            Dictionary<string, string> parameters = new Dictionary<string, string>(){
                     {"owner_id", ownerId.ToString()},
                     {"offset", "0"}
            };


            for (int i = 0; i < CommentsPostsPerTime && i < postIds.Count; i++)
            {
                parameters.Add("post_id" + (i + 1), postIds[i].ToString());
            }

            var req = new VKRequest<EntityList<Comment>>()
            {
                MethodName = "execute.get25Comments",
                Token = vk.CurrentToken,
                Parameters = parameters
            };
            Sleep();
            var result = (await vk.Executor.ExecAsync(req)).Response;
            return result;
        }






        private VKApi GetNewVkApi()
        {
            var api = new VKApi();
            api.AddToken(_tokenRepo.GetToken());
            return api;
        }


        private void Sleep()
        {
            Thread.Sleep(SleepMs);
        }


    }
}
