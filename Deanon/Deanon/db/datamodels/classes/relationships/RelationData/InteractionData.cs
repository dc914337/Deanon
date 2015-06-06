using System;

namespace Deanon.db.datamodels.classes.relationships.RelationData
{
	class InteractionData
	{
		public InteractionType interactionType { get; private set; }

		public String TypeName => interactionType.ToString();
		public int Weight => (int)interactionType;

		public InteractionData(InteractionType type)
		{
			interactionType = type;
		}
	}

	public enum InteractionType
	{
		Frientd = 1,
		Posted = 2,
		LeftComment = 3,
		LikedPost = 4,
		LikedComment = 5
	};
}
