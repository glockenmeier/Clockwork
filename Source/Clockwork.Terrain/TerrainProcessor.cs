using SiliconStudio.Core;
using SiliconStudio.Paradox.EntityModel;
using SiliconStudio.Paradox.Games;

namespace Clockwork.Terrain
{
    public sealed class TerrainComponent : EntityComponent
    {
        public static PropertyKey<TerrainComponent> Key = new PropertyKey<TerrainComponent>("Key", typeof(TerrainComponent));

        public override PropertyKey DefaultKey
        {
            get { return Key; }
        }

        public TerrainContent Content { get; set; }
    }

    public class TerrainProcessor : EntityProcessor<TerrainProcessor.AssociatedData>
    {
        public class AssociatedData
        {
            public TerrainComponent TerrainComponent;
        }

        public TerrainProcessor()
            : base(new PropertyKey[] { TerrainComponent.Key })
        {
        }

        protected override AssociatedData GenerateAssociatedData(Entity entity)
        {
            return new AssociatedData
            {
                TerrainComponent = entity.Get<TerrainComponent>()
            };
        }

        protected override void OnSystemAdd()
        {
        }

        protected override void OnSystemRemove()
        {
        }

        protected override void OnEntityAdding(Entity entity, AssociatedData data)
        {
        }

        protected override void OnEntityRemoved(Entity entity, AssociatedData data)
        {
        }

        public override void Update(GameTime time)
        {
        }
    }
}
