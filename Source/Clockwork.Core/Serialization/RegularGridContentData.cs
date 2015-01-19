using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization.Contents;

namespace Clockwork.Serialization
{
    [DataContract]
    [ContentSerializer(typeof(DataContentSerializer<RegularGridContentData>))]
    public class RegularGridContentData
    {
        public Rectangle Bounds;

        public Vector2 Origin;

        public Vector2 CellSize;

        public string NamePattern;
    }
}
