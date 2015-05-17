using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deanon.dumper
{
    class DumpingDepth
    {
        private Dictionary<EnterType, Depth> depths;

        public DumpingDepth(List<Depth> init)
        {
            depths = new Dictionary<EnterType, Depth>();
            foreach (var depth in init)
            {
                depths.Add(depth.type, depth);
            }
        }


        public bool Enter(EnterType type)
        {
            if (!CheckBottom(type))
            {
                LevelDown();
                return true;
            }
            else
                return false;
        }

        public void StepOut()
        {
            LevelUp();
        }

        private bool CheckBottom(EnterType type)
        {
            return depths[type].depth <= 0;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var depth in depths)
            {
                sb.AppendLine(RelationString.ToString(depth.Value.type) + " = " + depth.Value.depth);
            }

            return sb.ToString();
        }

        public bool IsBottom()
        {
            return depths.All(depth => depth.Value.depth <= 0);
        }

        private void LevelDown()
        {
            foreach (var d in depths)
            {
                d.Value.depth--;
            }
        }
        private void LevelUp()
        {
            foreach (var d in depths)
            {
                d.Value.depth++;
            }
        }
    }


    class Depth
    {
        public Depth(EnterType type, int depth)
        {
            this.type = type;
            this.depth = depth;
        }

        public EnterType type { get; set; }
        public int depth { get; set; }
    }
}
