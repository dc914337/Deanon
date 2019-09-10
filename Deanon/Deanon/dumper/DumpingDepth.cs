using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Deanon.dumper
{
    public class DumpingDepth
    {
        private readonly Dictionary<EnterType, Depth> depths;

        public DumpingDepth(List<Depth> init)
        {
            this.depths = new Dictionary<EnterType, Depth>();
            foreach (var depth in init)
            {
                this.depths.Add(depth.Type, depth);
            }
        }

        public bool Enter(EnterType type)
        {
            if (!this.CheckBottom(type))
            {
                this.LevelDown();
                return true;
            }
            else
            {
                return false;
            }
        }

        public void StepOut() => this.LevelUp();

        private bool CheckBottom(EnterType type) => this.depths[type].Value <= 0;

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var depth in this.depths)
            {
                sb.Append(RelationString.ToString(depth.Value.Type)).Append(" = ").Append(depth.Value.Value).AppendLine();
            }

            return sb.ToString();
        }

        public bool IsBottom() => this.depths.All(depth => depth.Value.Value <= 0);

        private void LevelDown()
        {
            foreach (var d in this.depths)
            {
                d.Value.Value--;
            }
        }

        private void LevelUp()
        {
            foreach (var d in this.depths)
            {
                d.Value.Value++;
            }
        }
    }
}
