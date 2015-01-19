using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Graphics;

namespace Clockwork.Terrain
{
    public struct TerrainPatchVertex : IVertex
    {
        public Vector2 Position;

        public static readonly int Size = 8;

        public static readonly VertexDeclaration Layout = new VertexDeclaration(
            VertexElement.Position<Vector2>());

        public VertexDeclaration GetLayout()
        {
            return Layout;
        }

        public void FlipWinding()
        {
        }
    }
}
