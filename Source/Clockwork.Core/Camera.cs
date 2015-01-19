using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Engine;
using System;

namespace Clockwork
{
    /// <summary>
    /// Helper class for representing and manipulating camera state.
    /// </summary>
    public class Camera
    {
        private const float maximumPitch = MathUtil.PiOverTwo * 0.99f;

        /// <summary>
        /// The eye position.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// The position the camera is oriented towards.
        /// </summary>
        public Vector3 Target { get; set; }

        /// <summary>
        /// The up-direction.
        /// </summary>
        public Vector3 Up { get; set; }

        /// <summary>
        /// The normalized viewing direction.
        /// </summary>
        public Vector3 ViewDirection
        {
            get { return Vector3.Normalize(Target - Position); }
            set { Target = Position + Vector3.Normalize(value) * (Target - Position).Length(); }
        }

        /// <summary>
        /// The ray along which the camera is viewing.
        /// </summary>
        public Ray ViewRay
        {
            get { return new Ray(Position, ViewDirection); }
        }

        /// <summary>
        /// A unit vector pointing to the right of the camera.
        /// </summary>
        public Vector3 Right
        {
            get { return Vector3.Normalize(Vector3.Cross(ViewDirection, Up)); }
        }

        /// <summary>
        /// The view direction projected to the horizontal plane, that is, the the plane orthogonal to the up-direction.
        /// </summary>
        public Vector3 HorizontalViewDirection
        {
            get { return Vector3.Normalize(Vector3.Cross(Up, Right)); }
        }

        /// <summary>
        /// The camera's local coordinate system, as if the viewing direction were orthogonal to the up-direction.
        /// </summary>
        public Matrix HorizontalFrame
        {
            get
            {
                return new Matrix
                {
                    Row1 = new Vector4(Right, 0),
                    Row2 = new Vector4(Up, 0),
                    Row3 = new Vector4(-HorizontalViewDirection, 0),
                    M44 = 1
                };
            }
        }

        /// <summary>
        /// The vertical field of view.
        /// </summary>
        public float FieldOfView { get; set; }

        /// <summary>
        /// The screen aspect ratio.
        /// </summary>
        public float AspectRatio { get; set; }

        /// <summary>
        /// The distance to the frustum near plane.
        /// </summary>
        public float NearPlane { get; set; }

        /// <summary>
        /// The distance to the frustum far plane.
        /// </summary>
        public float FarPlane { get; set; }

        /// <summary>
        /// Creates a new <see cref="Camera" /> instance.
        /// </summary>
        public Camera()
        {
            Target = -Vector3.UnitZ;
            Up = Vector3.UnitY;

            FieldOfView = (float)System.Math.PI / 6.0f;
            AspectRatio = 4.0f / 3.0f;
            NearPlane = 1.0f;
            FarPlane = 1000.0f;
        }

        /// <summary>
        /// The camera's view matrix.
        /// </summary>
        public Matrix View
        {
            get { return Matrix.LookAtRH(Position, Target, Up); }
        }

        /// <summary>
        /// The camera's projection matrix.
        /// </summary>
        public Matrix Projection
        {
            get { return Matrix.PerspectiveFovRH(FieldOfView, AspectRatio, NearPlane, FarPlane); }
        }

        /// <summary>
        /// Generates view and projeciton matrix.
        /// </summary>
        /// <param name="view">The camera's view matrix.</param>
        /// <param name="projection">The camera's projection matrix.</param>
        public void GetTransforms(out Matrix view, out Matrix projection)
        {
            view = Matrix.LookAtRH(Position, Target, Up);
            projection = Matrix.PerspectiveFovRH(FieldOfView, AspectRatio, NearPlane, FarPlane);
        }

        /// <summary>
        /// The view frustum.
        /// </summary>
        public BoundingFrustum Frustum
        {
            get { return new BoundingFrustum(View * Projection); }
        }

        /*
        public FrustumCameraParams Parameters
        {
            get
            {
                return new FrustumCameraParams
                {
                    AspectRatio = AspectRatio,
                    FOV = FieldOfView,
                    LookAtDir = ViewDirection,
                    Position = Position,
                    UpDir = Up,
                    ZFar = FarPlane,
                    ZNear = NearPlane
                };
            }
        }*/

        /// <summary>
        /// Gets the direction the camera would look towards, after being rotated by the given angles.
        /// </summary>
        /// <param name="yaw">The yaw angle.</param>
        /// <param name="pitch">The pitch angle.</param>
        /// <param name="roll">The roll angle.</param>
        /// <returns></returns>
        public Vector3 PredictViewDirection(float yaw, float pitch, float roll)
        {
            // TODO: Not rolling
            Vector3 direction = ViewDirection;

            float dot = Vector3.Dot(direction, Up);
            float currentPitch = (float)Math.Acos(MathUtil.Clamp(-dot, -1, 1)) - MathUtil.PiOverTwo;
            float newPitch = MathUtil.Clamp(currentPitch + pitch, -maximumPitch, maximumPitch);
            float allowedChange = newPitch - currentPitch;

            Vector3 right = Vector3.Normalize(Vector3.Cross(direction, Up));
            return Vector3.Transform(direction, Quaternion.RotationAxis(right, allowedChange) * Quaternion.RotationAxis(Up, yaw));
        }

        /// <summary>
        /// Rotates the camera around the current target.
        /// </summary>
        /// <param name="yaw">The yaw angle.</param>
        /// <param name="pitch">The pitch angle.</param>
        public void Orbit(float yaw, float pitch)
        {
            var distance = Vector3.Distance(Position, Target);
            Position = Target - PredictViewDirection(yaw, pitch, 0) * distance;
        }

        /// <summary>
        /// Rotates the camera around the eye position.
        /// </summary>
        /// <param name="yaw">The yaw angle.</param>
        /// <param name="pitch">The pitch angle.</param>
        /// <param name="roll">The roll angle.</param>
        public void Rotate(float yaw, float pitch, float roll)
        {
            var distance = Vector3.Distance(Position, Target);
            Target = Position + PredictViewDirection(yaw, pitch, roll);
        }

        /// <summary>
        /// Changes the eye position, without changing the viewing direction.
        /// </summary>
        /// <param name="position">The new eye position.</param>
        public void MoveTo(Vector3 position)
        {
            var direction = Target - Position;
            Position = position;
            Target = position + direction;
        }

        /// <summary>
        /// Moves the camera in it's local coordinate system.
        /// </summary>
        /// <param name="offset">The position offset.</param>
        public void Pan(Vector3 offset)
        {
            Pan(offset.X, offset.Y, offset.Z);
        }

        /// <summary>
        /// Moves the camera in it's local coordinate system.
        /// </summary>
        /// <param name="x">The offset in the x-direction.</param>
        /// <param name="y">The offset in the y-direction.</param>
        /// <param name="z">The offset in the z-direction.</param>
        public void Pan(float x, float y, float z)
        {
            Vector3 direction = Position - Target;
            direction.Normalize();
            Vector3 axis = Vector3.Cross(direction, Up);

            Vector3 translation = x * -axis + y * Up + z * direction;
            Position += translation;
            Target += translation;
        }

        /// <summary>
        /// Moves towards the target by dividing the distance to the target.
        /// </summary>
        /// <param name="factor">The value by which the distance is divided.</param>
        public void Zoom(float factor)
        {
            Position = Target + (Position - Target) / factor;
        }

        /// <summary>
        /// Moves towards the target, so as to just fit the given extents on the screen.
        /// </summary>
        /// <param name="extent"></param>
        public void ZoomTo(Vector2 extent)
        {
            float aspectRatio = extent.X / extent.Y;
            float effectiveHeight = aspectRatio > AspectRatio ? extent.X / AspectRatio : extent.Y;
            float distance = effectiveHeight * 0.5f / (float)Math.Tan(FieldOfView * 0.5f);

            Vector3 direction = Target - Position;
            direction.Normalize();

            Vector3 offset = direction * distance;
            Position = Target - offset;
        }

        /// <summary>
        /// Gets the extensts of the view frustum at the given distance.
        /// </summary>
        /// <param name="distance">The distance at which the extent is calculated.</param>
        /// <returns>The view frustum extents.</returns>
        public Vector2 GetExtent(float distance)
        {
            float height = (float)Math.Tan(FieldOfView * 0.5f);
            float width = height / AspectRatio;
            return new Vector2(width * distance, height * distance);
        }

        /// <summary>
        /// Gets the normalized view ray at the given screen coordinate.
        /// </summary>
        /// <param name="screenPosition">A screen position.</param>
        /// <returns>The view ray.</returns>
        public Ray GetScreenRay(Vector2 screenPosition)
        {
            var direction = Vector3.Unproject(new Vector3(screenPosition, 0), 0, 0, 1, 1, 0, 1, View * Projection) - Position;
            direction.Normalize();
            return new Ray(Position, direction); 
        }
    }

    public static class CameraComponentExtensions
    {
        /// <summary>
        /// Updates <see cref="SiliconStudio.Paradox.Endgine.CameraComponent"/> from a <see cref="Camera"/>.
        /// </summary>
        /// <param name="cameraComponent">The camera component</param>
        /// <param name="camera">The camera instance.</param>
        public static void Update(this CameraComponent cameraComponent, Camera camera)
        {
            cameraComponent.NearPlane = camera.NearPlane;
            cameraComponent.FarPlane = camera.FarPlane;
            cameraComponent.AspectRatio = camera.AspectRatio;
            cameraComponent.VerticalFieldOfView = camera.FieldOfView;

            if (cameraComponent.UseViewMatrix)
            {
                cameraComponent.ViewMatrix = camera.View;
            }
            else
            {
                cameraComponent.Entity.Transformation.Translation = camera.Position;

                if (cameraComponent.Target != null)
                {
                    cameraComponent.TargetUp = camera.Up;
                    cameraComponent.Target.Transformation.Translation = camera.Target;
                }
                else
                {
                    cameraComponent.Entity.Transformation.Rotation = Quaternion.RotationMatrix(camera.View);
                }
            }
        }
    }
}
