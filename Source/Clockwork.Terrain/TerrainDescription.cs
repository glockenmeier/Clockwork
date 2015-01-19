using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Contents;
using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Graphics;
using System.Collections.Generic;

namespace Clockwork.Terrain
{
    [DataContract]
    [ContentSerializer(typeof(DataContentSerializer<TerrainDescription>))]
    public class TerrainDescription
    {
        public int VerticesPerPatch;

        public Int2 VertexOverlap;

        public float PatchSize;

        public float HeightScale;

        public PixelFormat HeightMapFormat;

        public string HeightMapName;

        public QuadTree<TerrainTileData> Tree;

        public List<ContentReference<Material>> Materials;
    }
}
