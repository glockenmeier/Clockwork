using Clockwork.Serialization;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Serializers;

namespace Clockwork.Terrain
{
    [DataSerializer(typeof(TerrainTileSerializer))]
    public class TerrainTileData : QuadTreeContentTile
    {
        public float MinimumHeight;
        public float MaximumHeight;
    }

    public class TerrainTileSerializer : DataSerializer<TerrainTileData>
    {
        public override void PreSerialize(ref TerrainTileData obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                if (obj == null)
                {
                    obj = new TerrainTileData();
                }
            }
        }

        public override void Serialize(ref TerrainTileData obj, ArchiveMode mode, SerializationStream stream)
        {
            stream.Serialize(ref obj.MinimumHeight);
            stream.Serialize(ref obj.MaximumHeight);
        }
    }
}
