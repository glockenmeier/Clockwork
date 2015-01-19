
namespace Clockwork.Serialization
{
    public struct DataRange
    {
        public int Start;

        public int Length;

        public DataRange(int start, int length)
        {
            Start = start;
            Length = length;
        }
    }
}
