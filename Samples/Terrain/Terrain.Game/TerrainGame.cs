using BEPUphysics.Character;
using Clockwork.Atmosphere;
using Clockwork.Serialization;
using Clockwork.Terrain;
using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox;
using SiliconStudio.Paradox.DataModel;
using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Effects.Modules;
using SiliconStudio.Paradox.Effects.Modules.Processors;
using SiliconStudio.Paradox.Effects.Modules.Renderers;
using SiliconStudio.Paradox.Engine;
using SiliconStudio.Paradox.EntityModel;
using SiliconStudio.Paradox.Games;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Input;
using SiliconStudio.Paradox.UI;
using SiliconStudio.Paradox.UI.Renderers;
using System;
using System.Threading.Tasks;

namespace Terrain
{
    using Clockwork;
    using Clockwork.Physics;

    public class TerrainGame : Game
    {
        private CharacterController characterController;
        private PhysicsSystem physicsSystem;

        private Camera camera;
        private CameraComponent cameraComponent;
        private CharacterCameraController characterCameraController;
        private FreeCameraController freeCameraController;

        private TerrainModel terrain;
        private TerrainContent terrainContent;
        private TerrainPhysicsContent terrainPhysics;

        private LightComponent sunLight;
        private AtmosphereData atmosphere;

        public TerrainGame()
        {
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_10_1 };
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            Window.IsMouseVisible = false;

            GameSystems.Add(physicsSystem = new PhysicsSystem(Services));
            Entities.Processors.Add(new PhysicsProcessor());

            atmosphere = AtmosphereBuilder.Generate(GraphicsDevice, EffectSystem);

            CreateSunLight();

            camera = new Camera
            {
                Position = Vector3.UnitZ, //new Vector3(-400, 400, -400),
                Target = Vector3.Zero,// new Vector3(0, 200, 0),
                Up = Vector3.UnitY,
                FarPlane = 100000,
                NearPlane = 0.1f,
                AspectRatio = (float)GraphicsDevice.BackBuffer.Width / GraphicsDevice.BackBuffer.Height,
            };

            terrainContent = Asset.Load<TerrainContent>("terrain");
            terrain = new Clockwork.Terrain.TerrainModel(GraphicsDevice, terrainContent, camera);

            CreatePipeline();

            CreateCharacter(new Vector3(0, 200, -20));
            characterController = CreateCharacter(new Vector3(0, 200, 0)).GetOrCreate<PhysicsComponent>().CharacterController;

            terrainPhysics = Asset.Load<TerrainPhysicsContent>("terrain__COLLISION");
            terrainPhysics.Observers.Add(new RegularGridContentObserver { LoadingRange = 1000, UnloadingRange = 1500 });
            terrainPhysics.Observe();

            CreateCamera();

            Script.Add(ProcessInput);
            Script.Add(UpdateTerrain);
        }

        private void CreatePipeline()
        {
            var renderers = RenderSystem.Pipeline.Renderers;
            var width = GraphicsDevice.BackBuffer.Width;
            var height = GraphicsDevice.BackBuffer.Height;
            var viewport = new Viewport(0, 0, width, height);
            var clearColor = Color.Black;
            var effectName = "TerrainEffect";
            var prepassEffectName = "TerrainPrepassEffect";

            // Adds a light processor that will track all the entities that have a light component.
            // This will also handle the shadows (allocation, activation etc.).
            var lightProcessor = Entities.GetProcessor<LightShadowProcessor>();
            if (lightProcessor == null)
                Entities.Processors.Add(new DynamicLightShadowProcessor(GraphicsDevice, true));

            renderers.Add(new CameraSetter(Services));

            // Shadows
            var shadowMapPipeline = new RenderPipeline("ShadowMap");
            shadowMapPipeline.Renderers.Add(new ModelRenderer(Services, effectName + ".ShadowMapCaster").AddContextActiveLayerFilter().AddShadowCasterFilter());
            shadowMapPipeline.Renderers.Add(new TerrainRenderer(Services, "TerrainTerrainEffect.ShadowMapCaster", terrain));
            var shadowMapRenderer = new ShadowMapRenderer(Services, shadowMapPipeline);
            renderers.Add(shadowMapRenderer);

            // GBuffer pass
            var gBufferPipeline = new RenderPipeline("GBuffer");
            gBufferPipeline.Renderers.Add(new ModelRenderer(Services, effectName + ".ParadoxGBufferShaderPass").AddOpaqueFilter());
            gBufferPipeline.Renderers.Add(new TerrainRenderer(Services, "TerrainTerrainEffect.ParadoxGBufferShaderPass", terrain));
            var gBufferRenderProcessor = new GBufferRenderProcessor(Services, gBufferPipeline, GraphicsDevice.DepthStencilBuffer, false);
            renderers.Add(gBufferRenderProcessor);

            // Light prepass
            var lightPrePass = new LightingPrepassRenderer(Services, prepassEffectName, GraphicsDevice.DepthStencilBuffer.Texture, gBufferRenderProcessor.GBufferTexture);
            renderers.Add(lightPrePass);

            renderers.Add(new RenderTargetSetter(Services)
            {
                ClearColor = clearColor,
                EnableClearDepth = false,
                RenderTarget = GraphicsDevice.BackBuffer,
                DepthStencil = GraphicsDevice.DepthStencilBuffer,
                Viewport = viewport
            });

            renderers.Add(new RenderStateSetter(Services) { DepthStencilState = GraphicsDevice.DepthStencilStates.DepthRead });
            renderers.Add(new ModelRenderer(Services, effectName).AddOpaqueFilter());
            renderers.Add(new TerrainRenderer(Services, "TerrainTerrainEffect", terrain));

            renderers.Add(new RenderStateSetter(Services) { DepthStencilState = GraphicsDevice.DepthStencilStates.Default, RasterizerState = GraphicsDevice.RasterizerStates.CullBack });
            renderers.Add(new ModelRenderer(Services, effectName).AddTransparentFilter());

            // Blend atmoshpere inscatter on top
            renderers.Add(new AtmosphereRenderer(Services, atmosphere, sunLight));

            GraphicsDevice.Parameters.Set(RenderingParameters.UseDeferred, true);
        }

        private class RootRenderer : ElementRenderer
        {
            RenderTarget uiScreenRenderTarget;

            public RootRenderer(IServiceRegistry services, RenderTarget uiScreenRenderTarget)
                : base(services)
            {
                this.uiScreenRenderTarget = uiScreenRenderTarget;
            }

            public override void RenderColor(UIElement element, UIRenderingContext context)
            {
                context.RenderTarget = uiScreenRenderTarget;
                GraphicsDevice.SetRenderTarget(context.DepthStencilBuffer, uiScreenRenderTarget);
                base.RenderColor(element, context);
            }
        }

        private float sunZenith = MathUtil.RevolutionsToRadians(0.2f);

        private bool isMouseCentered = true;

        private void ToggleMouseCentering()
        {
            isMouseCentered = !isMouseCentered;
            IsMouseVisible = !isMouseCentered;
        }

        private async Task UpdateTerrain()
        {
            while (IsRunning)
            {
                await Script.NextFrame();
                terrain.Update(UpdateTime.Elapsed);
            }
        }

        private async Task ProcessInput()
        {
            while (IsRunning)
            {
                var oldMousePosition = Input.MousePosition;
                if (IsActive)
                    Input.SetMousePosition(Vector2.One * 0.5f);

                await Script.NextFrame();

                // Exit game
                if (Input.IsKeyPressed(Keys.Escape))
                    Exit();

                // Change sun direction
                if (Input.IsKeyDown(Keys.OemPlus))
                {
                    sunZenith = MathUtil.Mod2PI(sunZenith + MathUtil.Pi * (float)UpdateTime.Elapsed.TotalSeconds * 0.1f);
                }
                else if (Input.IsKeyDown(Keys.OemMinus))
                {
                    sunZenith = MathUtil.Mod2PI(sunZenith - MathUtil.Pi * (float)UpdateTime.Elapsed.TotalSeconds * 0.1f);
                }
                sunLight.LightDirection = new Vector3((float)Math.Sin(sunZenith), -(float)Math.Cos(sunZenith), 0);

                // Rotate character to camera
                var character = characterCameraController.Character;
                if (!characterCameraController.IsFree)
                    character.ViewDirection = camera.ViewDirection;

                // Switch between first person and third person view
                if (Input.IsKeyPressed(Keys.C))
                {
                    if (characterCameraController.Camera == null)
                    {
                        characterCameraController.Camera = camera;
                        freeCameraController.Camera = null;
                        character.Body.Position = camera.Position;
                        character.ViewDirection = camera.ViewDirection;
                        character.Body.LinearVelocity = Vector3.Zero;
                    }
                    else
                    {
                        characterCameraController.Camera = null;
                        freeCameraController.Camera = camera;
                    }
                }
            }
        }

        private Entity CreateCharacter(Vector3 position)
        {
            var entity = new Entity();

            var characterController = new CharacterController(position);
            characterController.StandingSpeed = 1.1f * 4;
            entity.GetOrCreate<PhysicsComponent>().CharacterController = characterController;
  
            Entities.Add(entity);
            return entity;
        }

        
        private void CreateSunLight()
        {
            // create the lights
            var directLightEntity = new Entity();
            sunLight = new LightComponent
            {
                Type = LightType.Directional,
                Color = new Color3(1, 1, 1),
                Deferred = true,
                Enabled = true,
                Intensity = 1.0f,
                LightDirection = new Vector3(5, -1, 5),
                Layers = RenderLayers.RenderLayerAll,
                ShadowMap = false,
                ShadowFarDistance = 1000,
                ShadowNearDistance = 1f,
                ShadowMapFilterType = ShadowMapFilterType.Nearest,
                ShadowMapCascadeCount = 4,
                ShadowMapMaxSize = 1024,
                ShadowMapMinSize = 1024,
            };
            directLightEntity.Add(sunLight);
            Entities.Add(directLightEntity);
        }

        private void CreateCamera()
        {
            var entity = new Entity("Camera");
            entity.GetOrCreate<AudioListenerComponent>();

            // Create and set the camera
            cameraComponent = new CameraComponent
            {
                //Target = new Entity { Name = "CameraTarget" },
                Entity = entity,
                UseViewMatrix = true
            };

            // Set Camera component for the render pipeline
            RenderSystem.Pipeline.SetCamera(cameraComponent);

            // Add entities to the scene
            Entities.Add(cameraComponent.Entity);
            //Entities.Add(cameraComponent.Target);

            characterCameraController = new CharacterCameraController(Services) { Character = characterController, Camera = camera };
            freeCameraController = new FreeCameraController(Services);

            Script.Add(characterCameraController);
            Script.Add(freeCameraController);
            Script.Add(new CharacterInput(Services) { Character = characterController, Camera = camera });

            Script.Add(async () =>
            {
                while (IsRunning)
                {
                    await Script.NextFrame();

                    cameraComponent.Update(camera);
                }
            });
        }
    }
}
