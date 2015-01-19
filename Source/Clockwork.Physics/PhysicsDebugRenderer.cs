using BEPUphysics;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.CollisionShapes.ConvexShapes;
using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Effects.Modules;
using SiliconStudio.Paradox.Extensions;
using SiliconStudio.Paradox.Graphics;
using System;
using System.Collections.Generic;

namespace Clockwork.Physics
{
    public class PhysicsDebugMesh
    {
        public int MaterialIndex;

        public Matrix LocalTransform;

        public VertexArrayObject VertexArrayObject;

        public MeshDraw MeshDraw;
    }

    public interface IDebugMeshFactory
    {
        bool CanCreate(object source);

        PhysicsDebugMesh Create(GraphicsDevice graphicsDevice, object source);
    }

    public abstract class DebugMeshFactoryBase<T> : IDebugMeshFactory
        where T : class
    {
        private Random random = new Random();

        public bool CanCreate(object source)
        {
            return source is T;
        }

        public PhysicsDebugMesh Create(GraphicsDevice graphicsDevice, object source)
        {
            T sourceAsT = source as T;

            if (sourceAsT == null)
                throw new ArgumentException(string.Format("Source object must be of type {0}", typeof(T)), "source");

            var meshDraw = new GeometricPrimitive(graphicsDevice, GetMeshData(sourceAsT)).ToMeshDraw();
            meshDraw.PrimitiveType = PrimitiveType.TriangleStrip;

            return new PhysicsDebugMesh
            {
                MaterialIndex = random.Next(),
                LocalTransform = GetLocalTransform(sourceAsT),
                MeshDraw = meshDraw,
                VertexArrayObject = VertexArrayObject.New(graphicsDevice, meshDraw.IndexBuffer, meshDraw.VertexBuffers)
            };
        }

        protected abstract GeometricMeshData<VertexPositionNormalTexture> GetMeshData(T source);

        protected virtual Matrix GetLocalTransform(T source)
        {
            return Matrix.Identity;
        }
    }

    public abstract class ConvexCollidableDebugMeshFactory<T> : DebugMeshFactoryBase<ConvexCollidable<T>>
        where T : ConvexShape
    {
        protected override GeometricMeshData<VertexPositionNormalTexture> GetMeshData(ConvexCollidable<T> source)
        {
            return GetMeshData(source.Shape);
        }

        protected abstract GeometricMeshData<VertexPositionNormalTexture> GetMeshData(T shape);
    }

    public class CapsuleDebugMeshFactory : ConvexCollidableDebugMeshFactory<CapsuleShape>
    {
        protected override GeometricMeshData<VertexPositionNormalTexture> GetMeshData(CapsuleShape shape)
        {
            return GeometricPrimitive.Capsule.New(shape.Length, shape.Radius);
        }
    }

    public class TerrainDebugMeshFactory : DebugMeshFactoryBase<Terrain>
    {
        protected override GeometricMeshData<VertexPositionNormalTexture> GetMeshData(Terrain source)
        {
            var heights = source.Shape.Heights;
            int width = heights.GetLength(0);
            int height = heights.GetLength(1);

            var vertices = new VertexPositionNormalTexture[width * height];
            var indices = new int[(height - 1) * (width * 2 + 1)];

            int vertexCount = 0;
            int indexCount = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    vertices[vertexCount++] = new VertexPositionNormalTexture(new Vector3(x, heights[x, y], y), Vector3.UnitY, Vector2.Zero);
                }
            }

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int vbase = width * y + x;
                    indices[indexCount++] = vbase + width;
                    indices[indexCount++] = vbase;
                }

                indices[indexCount++] = -1;
            }

            return new GeometricMeshData<VertexPositionNormalTexture>(vertices, indices, false);
        }

        protected override Matrix GetLocalTransform(Terrain source)
        {
            var m = source.WorldTransform.LinearTransform;
            var t = source.WorldTransform.Translation;
            return new Matrix(m.M11, m.M12, m.M13, 0, m.M21, m.M22, m.M23, 0, m.M31, m.M32, m.M33, 0, t.X, t.Y, t.Z, 1f);
        }

        private static void SetHeight(ref VertexPositionNormalTexture vertex, float height)
        {
            vertex.Position += new Vector3(0, height, 0);
        }
    }

    public class PhysicsDebugRenderer : Renderer
    {
        private List<IDebugMeshFactory> meshFactories;
        private Dictionary<object, PhysicsDebugMesh> meshes;
        private Effect effect;
        private Space space;
        private Color[] colors;

        public PhysicsDebugRenderer(IServiceRegistry services, string effectName) : base(services)
        {
            effect = EffectSystem.LoadEffect(effectName);
            colors = new[] 
            { 
                new Color(255, 216, 0),
                new Color(79, 200, 255),
                new Color(255, 0, 0),
                new Color(177, 0, 254),
                new Color(255, 130, 151),
                new Color(254, 106, 0),
                new Color(168, 165, 255),
                new Color(0, 254, 33)
            };

            meshes = new Dictionary<object, PhysicsDebugMesh>();

            meshFactories = new List<IDebugMeshFactory>
            {
                new CapsuleDebugMeshFactory(),
                new TerrainDebugMeshFactory()
            };
        }

        public override void Load()
        {
            Pass.StartPass += Render;
        }

        public void Add(object source)
        {
            foreach (var factory in meshFactories)
            {
                if (factory.CanCreate(source))
                {
                    var mesh = factory.Create(GraphicsDevice, source);
                    meshes.Add(source, mesh);
                    break;
                }
            }
        }

        public void Remove(object source)
        {
            meshes.Remove(source);
        }

        private void Render(RenderContext renderContext)
        {
            foreach (var mesh in meshes.Values)
            {
                int colorIndex = mesh.MaterialIndex % colors.Length;
                effect.Parameters.Set(SpriteEffectKeys.Color, colors[colorIndex]);
                effect.Parameters.Set(TransformationKeys.World, mesh.LocalTransform);
                
                effect.Apply(renderContext.CurrentPass.Parameters);

                GraphicsDevice.SetRasterizerState(GraphicsDevice.RasterizerStates.CullNone);
                GraphicsDevice.SetVertexArrayObject(mesh.VertexArrayObject);
                GraphicsDevice.DrawIndexed(mesh.MeshDraw.PrimitiveType, mesh.MeshDraw.DrawCount, mesh.MeshDraw.StartLocation);
            }
        }

        public override void Unload()
        {
            Pass.StartPass -= Render;
        }
    }
}
