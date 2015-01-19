
namespace Clockwork.Serialization
{
    public interface IVisibleRanges
    {
        int Count { get; }

        float this[int level] { get; }

        /*
        float GetRange(int level);

        float GetStartThreshold(int level);
         */
    }
}
