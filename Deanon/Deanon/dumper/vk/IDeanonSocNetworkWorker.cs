using System.Collections.Generic;
using System.Threading.Tasks;
using Deanon.db.datamodels.classes.entities;
using kasthack.vksharp.DataTypes.Entities;

namespace Deanon.dumper.vk
{
    public interface IDeanonSocNetworkWorker
    {
        Task<Person> GetPerson(int userId);
        Task<List<Post>> GetAllPosts(int id);
        Task<List<Comment>> GetAllCommentsForPosts(int id, List<Post> posts);
        Task<List<Person>> GetPeople(int[] postedPFriendsIds);
        Task<List<int>> GetAllPeopleLiked(int id, List<Post> posts, List<Comment> comments);
        Task<List<Person>> GetFollowers(Person user);
        Task<List<Person>> GetFriends(Person user);
    }
}
