using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Deanon.db.datamodels.classes.entities;

namespace Deanon.db.datamodels
{
	static class Mapper
	{
		private const String VkUrlPattern = "https://vk.com/id{0}";
		public static Person MapPerson(VKSharp.Core.Entities.User vkUser)
		{
			return new Person()
			{
				Id = vkUser.Id,
				Name = vkUser.FirstName,
				Surname = vkUser.LastName,
				Url = String.Format(VkUrlPattern, vkUser.Id)
			};
		}

	}
}
