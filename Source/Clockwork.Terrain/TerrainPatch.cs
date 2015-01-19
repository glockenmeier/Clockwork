using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Graphics;

namespace Clockwork.Terrain
{
     public class TerrainPatch : ComponentBase
    {
        private Buffer vertexBuffer;
        private Buffer indexBuffer;
        private Buffer instanceBuffer;
        private VertexArrayObject vertexArrayObjectInstancing;
        private VertexArrayObject vertexArrayObject;

        public TerrainPatch(GraphicsDevice graphics, int tesselation, int instanceCount)
        {
            var lineWidth = tesselation + 1;
            var vertices = new Vector2[lineWidth * lineWidth];
            var indices = new int[tesselation * (lineWidth * 2 + 1)];

            int vertexCount = 0;
            int indexCount = 0;

            for (int y = 0; y < lineWidth; y++)
            {
                for (int x = 0; x < lineWidth; x++)
                {
                    vertices[vertexCount++] = new Vector2(y, x) / tesselation;
                }
            }

            for (int y = 0; y < tesselation; y++)
            {
                for (int x = 0; x < lineWidth; x++)
                {
                    int vbase = lineWidth * y + x;
                    indices[indexCount++] = vbase;
                    indices[indexCount++] = vbase + lineWidth;
                }
                indices[indexCount++] = -1;
            }

            indexBuffer = Buffer.Index.New(graphics, indices).DisposeBy(this);
            vertexBuffer = Buffer.Vertex.New(graphics, vertices).DisposeBy(this);
            instanceBuffer = Buffer.Vertex.New<TerrainTileInfo>(graphics, instanceCount, GraphicsResourceUsage.Dynamic).DisposeBy(this);

            var indexBufferBinding = new IndexBufferBinding(indexBuffer, true, indices.Length);
            var vertexBufferBinding = new VertexBufferBinding(vertexBuffer, TerrainPatchVertex.Layout, vertices.Length);
            var instanceBufferBinding = new VertexBufferBinding(instanceBuffer, TerrainTileInfo.Layout, instanceCount);

            vertexArrayObjectInstancing = VertexArrayObject.New(graphics, indexBufferBinding, vertexBufferBinding, instanceBufferBinding);
            vertexArrayObject = VertexArrayObject.New(graphics, indexBufferBinding, vertexBufferBinding);
        }

        public unsafe void Draw(GraphicsDevice graphics, TerrainTileInfo[] instances, int instanceCount, bool useQuads = true)
        {
            var primitiveType = useQuads ? PrimitiveType.LineStripWithAdjacency : PrimitiveType.TriangleStrip;

            fixed (void* p = &instances[0])
            {
                instanceBuffer.SetData(new DataPointer(p, instanceCount * TerrainTileInfo.Size));
            }

            graphics.SetVertexArrayObject(vertexArrayObjectInstancing);
            graphics.DrawIndexedInstanced(primitiveType, indexBuffer.ElementCount, instanceCount);
        }

        public void Draw(GraphicsDevice graphics, bool useQuads = true)
        {
            var primitiveType = useQuads ? PrimitiveType.LineStripWithAdjacency : PrimitiveType.TriangleStrip;

            graphics.SetVertexArrayObject(vertexArrayObject);
            graphics.DrawIndexed(primitiveType, indexBuffer.ElementCount);
        }
    }
}
