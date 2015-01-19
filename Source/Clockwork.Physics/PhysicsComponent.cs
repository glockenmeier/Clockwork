using BEPUphysics.Character;
using SiliconStudio.Core;
using SiliconStudio.Paradox.EntityModel;
using System.Collections.Generic;

namespace Clockwork.Physics
{
    public class PhysicsElement
    {
        public BEPUphysics.Entities.Entity Entity;
        public string LinkedBoneName;
    }

    public sealed class PhysicsComponent : EntityComponent
    {
        public static PropertyKey<PhysicsComponent> Key = new PropertyKey<PhysicsComponent>("Key", typeof(PhysicsComponent));

        public override PropertyKey DefaultKey
        {
            get { return Key; }
        }

        public List<PhysicsElement> Elements { get; private set; }

        public CharacterController CharacterController { get; set; }

        public PhysicsComponent()
        {
            Elements = new List<PhysicsElement>();
        }
    }
}
