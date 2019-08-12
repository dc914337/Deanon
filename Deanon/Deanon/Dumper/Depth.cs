namespace Deanon.dumper
{

    public class Depth
    {
        public Depth(EnterType type, int depth)
        {
            this.Type = type;
            this.Value = depth;
        }

        public EnterType Type { get; set; }
        public int Value { get; set; }
    }
}
