using Clockwork.Physics;
using Clockwork.Serialization;
using SiliconStudio.Core;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Contents;
using SiliconStudio.Core.Serialization.Converters;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Clockwork.Terrain
{
    public struct MaterialVertex
    {
        public float StaticFriction;
        public float DynamicFriction;
        public float Bounciness;
    }

    public class TerrainPhysicsPage : ContentTile
    {
        //public MaterialVertex[,] Vertices;
        public BEPUphysics.BroadPhaseEntries.Terrain Collider;
    }

    public class TerrainPhysicsConverter : DataConverter<RegularGridContentData, TerrainPhysicsContent>
    {
        public override void ConvertFromData(ConverterContext converterContext, RegularGridContentData data, ref TerrainPhysicsContent obj)
        {
            ServiceRegistry registry = converterContext.Tags.Get<ServiceRegistry>(ServiceRegistry.ServiceRegistryKey);
            obj = new TerrainPhysicsContent(registry, data);
        }

        public override void ConvertToData(ConverterContext converterContext, ref RegularGridContentData data, TerrainPhysicsContent obj)
        {
            throw new NotSupportedException();
        }

        [ModuleInitializer]
        internal static void Initialize()
        {
            ConverterContext.RegisterConverter<RegularGridContentData, TerrainPhysicsContent>(new TerrainPhysicsConverter());
        }
    }

    [ContentSerializer(typeof(DataContentConverterSerializer<TerrainPhysicsContent>))]
    public class TerrainPhysicsContent : RegularGridContent<TerrainPhysicsPage>
    {
        private PhysicsSystem physicsSystem;

        public TerrainPhysicsContent(IServiceRegistry serviceRegistry, RegularGridContentData data)
            : base(serviceRegistry, data)
        {
            physicsSystem = serviceRegistry.GetServiceAs<PhysicsSystem>();
        }

        private Stream stream;
        private BinarySerializationReader reader;

        protected override async Task<IDisposable> OpenAsync()
        {
            stream = await Task.Run(() => Asset.OpenAsStream(Data.NamePattern, StreamFlags.Seekable));
            reader = new BinarySerializationReader(stream);

            return new AnonymousDisposable(() =>
            {
                stream.Dispose();
                stream = null;
                reader = null;
            });
        }

        protected override async Task LoadTileAsync(Int2 key, int physicalOffset)
        {
            var tile = Tiles[key];

            int index = Data.Bounds.Width * key.Y + key.X;
            stream.Position = index * 4;
            int bufferOffset = reader.ReadInt32();

            if (bufferOffset < 0)
                return;

            stream.Position = bufferOffset;

            var heightMap = reader.Read<HeightMap>();
            //var heightMap = await Task.Run(() => reader.Read<HeightMap>());
            
            Vector3 offset = new Vector3(
                (key.X - Data.Bounds.Width / 2) * Data.CellSize.X, 0,
                (key.Y - Data.Bounds.Height / 2) * Data.CellSize.Y);

            var transform = new BEPUutilities.AffineTransform(new BEPUutilities.Matrix3x3(
                0, 0, Data.CellSize.X / (heightMap.Height - 1),
                0, 1, 0,
                Data.CellSize.Y / (heightMap.Width - 1), 0, 0),
                offset);

            tile.Collider = new BEPUphysics.BroadPhaseEntries.Terrain(heightMap.Data, transform);
            //collider.Material = new BEPUphysics.Materials.Material();
            //collider.Material.Tag = page;

            physicsSystem.Space.Add(tile.Collider);
        }

        protected override void OnUnload(Int2 key)
        {
            physicsSystem.Space.Remove(Tiles[key].Collider);
            base.OnUnload(key);
        }
    }
}
