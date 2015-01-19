using BEPUphysics;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.Character;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUutilities;
using SiliconStudio.Core;
using SiliconStudio.Paradox;
using SiliconStudio.Paradox.Input;
using System;
using System.Threading.Tasks;

namespace Clockwork
{
    using SiliconStudio.Core.Mathematics;

    public class CharacterCameraController : Script
    {
        private float distance;
        private Vector3 targetOffset;
        private Vector2 desiredRotation;
        private float desiredDistance;
        private TimeSpan cameraFreeTime;
        private ConeShape cameraShape;
        private Vector3 currentPosition;

        public CharacterController Character { get; set; }

        public Camera Camera { get; set; }

        public bool IsFirstPerson = true;

        public bool IsCombatMode;

        public bool IsFree;

        public Vector3 FirstPersonEyePosition = new Vector3(0, 1.4f, -0.4f);

        public Vector3 PeacefulModeOffset = new Vector3(0.7f, 1.3f, 0);

        public Vector3 VirtualEyePosition = Vector3.UnitY * 2.2f;

        public float MinimumDistance = 1f;

        public float MaximumDistance = 10.0f;

        public float DistanceAdaptationRate = 5;

        public float DirectionAdaptationRate = float.PositiveInfinity; // 15

        public float OffsetAdaptationRate = 3;

        public float ErrorCorrectionRate = 10;

        public Vector3 DesiredViewDirection { get; private set; }

        public CharacterCameraController(IServiceRegistry registry) : base(registry)
        {
            cameraShape = new ConeShape(1, 1);
        }

        private void Zoom(float zoom)
        {
            if (!IsFirstPerson || IsFree)
            {
                desiredDistance -= (zoom * (MaximumDistance - MinimumDistance) / 10); ;
                if (desiredDistance < MinimumDistance)
                {
                    IsFirstPerson = true;
                    distance = desiredDistance = MinimumDistance;
                }
                else if (desiredDistance > MaximumDistance)
                {
                    desiredDistance = MaximumDistance;
                }
            }
            else if (zoom < 0)
            {
                IsFirstPerson = false;
                distance = desiredDistance = MinimumDistance;
            }
        }

        public void ProcessInput(TimeSpan elapsedTime)
        {
            // Yaw and pitch
            desiredRotation += (Vector2.One * 0.5f/*oldMousePosition*/ - Input.MousePosition) * 3;

            // Zooming
            float zoom = Input.MouseWheelDelta / 120; 

            if (Input.IsKeyDown(Keys.PageUp))
                zoom += (float)elapsedTime.TotalSeconds * 10;
            else if (Input.IsKeyDown(Keys.PageDown))
                zoom -= (float)elapsedTime.TotalSeconds * 10;

            Zoom(zoom);

            // Perspective
            if (Input.IsKeyPressed(Keys.F))
            {
                    cameraFreeTime = TimeSpan.Zero;
                    IsFree = true;
            }
            else if (Input.IsKeyReleased(Keys.F))
            {
                if (cameraFreeTime < TimeSpan.FromSeconds(0.2))
                {
                    IsFirstPerson = !IsFirstPerson;
                }

                IsFree = false;
                //Camera.ViewDirection = Character.ViewDirection;
            }
        }

        private static float GetAdaptionRate(TimeSpan elapsedTime, float adaptationRate)
        {
            return 1.0f - (float)Math.Exp(-elapsedTime.TotalSeconds * adaptationRate);
        }

        public void Update(TimeSpan elapsedTime)
        {
            cameraFreeTime += elapsedTime;

            float directionRate = GetAdaptionRate(elapsedTime, DirectionAdaptationRate);
            float distanceRate = GetAdaptionRate(elapsedTime, DistanceAdaptationRate);
            float offsetRate = GetAdaptionRate(elapsedTime, OffsetAdaptationRate);
            float errorCorrectionRate = GetAdaptionRate(elapsedTime, ErrorCorrectionRate);

            Camera.Up = -Character.Down;

            if (!IsFree && (IsFirstPerson || IsCombatMode || Character.HorizontalMotionConstraint.MovementDirection.LengthSquared() > 0))
            {
                DesiredViewDirection = Camera.PredictViewDirection(desiredRotation.X, desiredRotation.Y, 0);
            }

            Vector2 rotationIncrement = desiredRotation * directionRate;
            desiredRotation -= rotationIncrement;
            Camera.Rotate(rotationIncrement.X, rotationIncrement.Y, 0);

            Vector3 position = (Vector3)(Character.Body.Position + Character.Down * Character.Body.Height * 0.5f);
            //System.Diagnostics.Debug.WriteLine(errorCorrectionRate + " " + (position - currentPosition));
//            position = currentPosition = Vector3.Lerp(currentPosition, position, errorCorrectionRate);
            
            //float error = Vector3.Dot(position - Camera.Position, Camera.Up);
            //error = MathUtil.Clamp(error, -Character.StepManager.MaximumStepHeight, Character.StepManager.MaximumStepHeight);

            if (IsFirstPerson && !IsFree)
            {
                Camera.MoveTo(position + Vector3.TransformNormal(FirstPersonEyePosition, Camera.HorizontalFrame));
            }
            else
            {
                Vector3 desiredTargetOffset = IsCombatMode ? VirtualEyePosition : PeacefulModeOffset;
                targetOffset = Vector3.Lerp(targetOffset, desiredTargetOffset, offsetRate);
                Vector3 targetPosition = position + Vector3.TransformNormal(targetOffset, Camera.HorizontalFrame);

                // Update the current distance smoothly
                distance = MathUtil.Lerp(distance, desiredDistance, distanceRate);

                // Check if there is something solid behind the target position and in front of the desired eyePosition
                // If so, put the camera in front of the obstacle, so we don't look through it
                var backRay = new Ray(targetPosition, -Camera.ViewDirection);
                //Camera.MoveTo(GetUnoccludedPositionRay(backRay, distance));
                Camera.MoveTo(GetUnoccludedPositionSweep(backRay, distance));
            }
        }

        private Vector3 GetUnoccludedPositionSweep(Ray backRay, float maximumDistance)
        {
            cameraShape.Radius = Camera.GetExtent(1.0f).Length();
            BEPUutilities.Vector3 sweep = backRay.Direction * desiredDistance;
            Quaternion orientation = Quaternion.RotationAxis(Vector3.Cross(Vector3.UnitY, backRay.Direction), (float)Math.Acos(Vector3.Dot(Vector3.UnitY, backRay.Direction)));
            RigidTransform transform = new RigidTransform(backRay.Position - backRay.Direction, orientation);

            RayCastResult result;
            Character.Space.ConvexCast(cameraShape, ref transform, ref sweep, IsObstacle, out result);

            if (result.HitObject != null)
            {
                return backRay.Position + backRay.Direction * result.HitData.T * desiredDistance;
            }
            else
            {
                return backRay.Position + backRay.Direction * maximumDistance;
            }
        }

        private Vector3 GetUnoccludedPositionRay(Ray backRay, float maximumDistance)
        {
            RayCastResult result;
            Character.Space.RayCast(backRay, maximumDistance, IsObstacle, out result);

            if (result.HitObject != null)
            {
                return result.HitData.Location;
            }
            else
            {
                return backRay.Position + backRay.Direction * maximumDistance;
            }
        }

        private bool IsObstacle(BroadPhaseEntry collider)
        {
            return collider != Character.Body.CollisionInformation;
        }

        Vector2 oldMousePosition;

        public override async Task Execute()
        {
            while (Game.IsRunning)
            {
                oldMousePosition = Input.MousePosition;

                await Script.NextFrame();

                if (Character != null && Camera != null)
                {
                    ProcessInput(Game.UpdateTime.Elapsed);
                    Update(Game.UpdateTime.Elapsed);
                }
            }
        }
    }
}
