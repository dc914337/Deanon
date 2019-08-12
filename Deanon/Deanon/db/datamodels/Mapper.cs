using Deanon.db.datamodels.classes.entities;
using kasthack.vksharp.DataTypes.Entities;

namespace Deanon.db.datamodels
{
    internal static class Mapper
    {
        private const string VkUrlPattern = "https://vk.com/id{0}";

        public static Person MapPerson(User vkUser) => new Person()
        {
            Id = vkUser.Id,
            Name = vkUser.FirstName,
            Surname = vkUser.LastName,
            Url = string.Format(VkUrlPattern, vkUser.Id),
            Deleted = vkUser.Deactivated != null
        };
    }
}
