using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Graphics;

namespace Clockwork.Terrain
{
    public struct TerrainTileInfo : IVertex
    {
        public Vector2 Offset;

        public Vector2 ParentOffset;

        public float Scale;

        public float Level;

        public float TextureIndex;

        public float ParentTextureIndex;

        public static readonly int Size = 32;

        public static readonly VertexDeclaration Layout = new VertexDeclaration(new VertexElement[]
        {
            new VertexElement("OFFSET", PixelFormat.R32G32_Float),
            new VertexElement("PARENT_OFFSET", PixelFormat.R32G32_Float),
            new VertexElement("SCALE", PixelFormat.R32_Float),
            new VertexElement("LEVEL", PixelFormat.R32_Float),
            new VertexElement("INDEX", PixelFormat.R32_Float),
            new VertexElement("PARENT_INDEX", PixelFormat.R32_Float),
        }, 1);

        public void FlipWinding()
        {
        }

        public VertexDeclaration GetLayout()
        {
            return Layout;
        }
    }
}
