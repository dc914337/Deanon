namespace Deanon.db.datamodels.classes.relationships.RelationData
{
    public class InteractionData
	{
        public InteractionType interactionType { get; }

        public string TypeName => this.interactionType.ToString();
		public int Weight => (int)this.interactionType;

        public InteractionData(InteractionType type) => this.interactionType = type;
    }
}
