using Deanon.db.datamodels.classes.entities;
using Deanon.db.datamodels.classes.relationships.RelationData;
using Neo4jClient;

namespace Deanon.db.datamodels.classes.relationships
{
    class Friend : Relationship, IRelationshipAllowingSourceNode<Person>, IRelationshipAllowingTargetNode<Person>
    {
        public Friend(NodeReference targetNode, InteractionData data) : base(targetNode, data)
        {

        }

        public override string RelationshipTypeKey
        {
            get
            {
                return "FRIENDS_WITH";
            }
        }
    }
}
