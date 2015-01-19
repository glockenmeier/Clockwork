using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Engine;
using SiliconStudio.Paradox.EntityModel;
using SiliconStudio.Paradox.Games;
using System;

namespace Clockwork.Physics
{
    public class PhysicsProcessor : EntityProcessor<PhysicsProcessor.AssociatedData>
    {
        private PhysicsSystem physicsSystem;

        public class AssociatedData
        {
            public PhysicsComponent PhysicsComponent;
            public TransformationComponent TransformationComponent;
            //public ModelComponent ModelComponent;
        }

        public PhysicsProcessor()
            : base(new PropertyKey[] { TransformationComponent.Key, PhysicsComponent.Key })
        {
        }

        protected override AssociatedData GenerateAssociatedData(Entity entity)
        {
            return new AssociatedData
            {
                TransformationComponent = entity.Transformation,
                PhysicsComponent = entity.Get<PhysicsComponent>()
            };
        }

        protected override void OnSystemAdd()
        {
            physicsSystem = Services.GetSafeServiceAs<PhysicsSystem>();
        }

        protected override void OnSystemRemove()
        {
            physicsSystem = null;
        }

        protected override void OnEntityAdding(Entity entity, AssociatedData data)
        {
            foreach (var element in data.PhysicsComponent.Elements)
            {
                element.Entity.Tag = entity;
                physicsSystem.Space.Add(element.Entity);
            }

            var characterController = data.PhysicsComponent.CharacterController;
            if (characterController != null)
                physicsSystem.Space.Add(characterController);
        }

        protected override void OnEntityRemoved(Entity entity, AssociatedData data)
        {
            var characterController = data.PhysicsComponent.CharacterController;
            if (characterController != null)
                physicsSystem.Space.Remove(characterController);

            foreach (var element in data.PhysicsComponent.Elements)
            {
                physicsSystem.Space.Remove(element.Entity);
                element.Entity.Tag = null;
            }
        }

        public override void Update(GameTime time)
        {
            //physicsSystem.Update(time);

            foreach (var entity in enabledEntities)
            {
                var transformation = entity.Value.TransformationComponent;

                var characterController = entity.Value.PhysicsComponent.CharacterController;
                if (characterController != null)
                {
                    transformation.Translation = characterController.Body.Position + characterController.Down * characterController.Body.Height * 0.5f;
                    transformation.Rotation = Quaternion.RotationY(
                        (float)Math.Atan2(-characterController.HorizontalMotionConstraint.MovementDirection.X, characterController.HorizontalMotionConstraint.MovementDirection.Y) +
                        (float)Math.Atan2(-characterController.ViewDirection.X, -characterController.ViewDirection.Z));

                    if (!transformation.UseTRS)
                    {
                        transformation.WorldMatrix =
                            Matrix.RotationQuaternion(transformation.Rotation) *
                            Matrix.Translation(transformation.Translation);

                        if (transformation.Parent != null /*|| transformation.isSpecialRoot*/)
                        {
                            var toLocalSpace = transformation.Parent.WorldMatrix;
                            toLocalSpace.Invert();
                            transformation.LocalMatrix = transformation.WorldMatrix * toLocalSpace;
                        }
                    }
                }

                foreach (var element in entity.Value.PhysicsComponent.Elements)
                {
                    var physicsEntity = element.Entity;

                    if (element.LinkedBoneName == null)
                    {
                        if (transformation.UseTRS)
                        {
                            transformation.Translation = physicsEntity.Position;
                            transformation.Rotation = physicsEntity.Orientation;
                        }
                        else
                        {
                            transformation.WorldMatrix = physicsEntity.WorldTransform;

                            if (transformation.Parent != null /*|| transformation.isSpecialRoot*/)
                            {
                                var toLocalSpace = transformation.Parent.WorldMatrix;
                                toLocalSpace.Invert();
                                transformation.LocalMatrix = transformation.WorldMatrix * toLocalSpace;
                            }
                        }
                    }
                }
            }
        }
    }
}
