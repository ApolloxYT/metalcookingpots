﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetalPots.Blocks;
using MetalPots.System.Cooking;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace MetalPots.BlockEntityRenderer
{
    internal class MetalPotInFirepitRenderer : IInFirepitRenderer
    {
        public double RenderOrder => 0.5;
        public int RenderRange => 20;

        ICoreClientAPI capi;
        MultiTextureMeshRef potWithFoodRef;
        MultiTextureMeshRef potRef;
        MultiTextureMeshRef lidRef;
        BlockPos pos;
        float temp;

        ILoadedSound cookingSound;

        bool isInOutputSlot;
        Matrixf ModelMat = new Matrixf();

        public MetalPotInFirepitRenderer(ICoreClientAPI capi, ItemStack stack, BlockPos pos, bool isInOutputSlot)
        {
            this.capi = capi;
            this.pos = pos;
            this.isInOutputSlot = isInOutputSlot;


            MPBlockCookedContainer potBlock = capi.World.GetBlock(stack.Collectible.CodeWithVariant("type", "cooked")) as MPBlockCookedContainer;

            if (isInOutputSlot)
            {
                MPMealMeshCache meshcache = capi.ModLoader.GetModSystem<MPMealMeshCache>();

                potWithFoodRef = meshcache.GetOrCreateMealInContainerMeshRef(potBlock, potBlock.GetCookingRecipe(capi.World, stack), potBlock.GetNonEmptyContents(capi.World, stack), new Vec3f(0, 6.0f / 16f, 0));
            }
            else
            {
                string basePath = potBlock.Code.PathStartsWith("dirtymetalpot") ? "metalpots:shapes/block/dirty-metalpot-" : "metalpots:shapes/block/metalpot-";
                MeshData potMesh;
                capi.Tesselator.TesselateShape(potBlock, Shape.TryGet(capi, basePath + "opened-empty-withtrivet.json"), out potMesh);
                potRef = capi.Render.UploadMultiTextureMesh(potMesh);

                MeshData lidMesh;
                capi.Tesselator.TesselateShape(potBlock, Shape.TryGet(capi, "metalpots:shapes/block/metalpot-part-lid.json"), out lidMesh);
                lidRef = capi.Render.UploadMultiTextureMesh(lidMesh);
            }
        }

        public void Dispose()
        {
            potRef?.Dispose();
            lidRef?.Dispose();

            cookingSound?.Stop();
            cookingSound?.Dispose();
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            IRenderAPI rpi = capi.Render;
            Vec3d camPos = capi.World.Player.Entity.CameraPos;

            rpi.GlDisableCullFace();
            rpi.GlToggleBlend(true);

            IStandardShaderProgram prog = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);

            prog.DontWarpVertices = 0;
            prog.AddRenderFlags = 0;
            prog.RgbaAmbientIn = rpi.AmbientColor;
            prog.RgbaFogIn = rpi.FogColor;
            prog.FogMinIn = rpi.FogMin;
            prog.FogDensityIn = rpi.FogDensity;
            prog.RgbaTint = ColorUtil.WhiteArgbVec;
            prog.NormalShaded = 1;
            prog.ExtraGodray = 0;
            prog.SsaoAttn = 0;
            prog.AlphaTest = 0.05f;
            prog.OverlayOpacity = 0;


            prog.ModelMatrix = ModelMat
                .Identity()
                .Translate(pos.X - camPos.X + 0.001f, pos.Y - camPos.Y, pos.Z - camPos.Z - 0.001f)
                .Translate(0f, 1 / 16f, 0f)
                .Values
            ;

            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;

            rpi.RenderMultiTextureMesh(potRef == null ? potWithFoodRef : potRef, "tex");

            if (!isInOutputSlot)
            {
                float origx = GameMath.Sin(capi.World.ElapsedMilliseconds / 300f) * 5 / 16f;
                float origz = GameMath.Cos(capi.World.ElapsedMilliseconds / 300f) * 5 / 16f;

                float cookIntensity = GameMath.Clamp((temp - 50) / 50, 0, 1);

                prog.ModelMatrix = ModelMat
                    .Identity()
                    .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
                    .Translate(0, 13.0f / 16f, 0)
                    .Translate(-origx, 0, -origz)
                    .RotateX(cookIntensity * GameMath.Sin(capi.World.ElapsedMilliseconds / 50f) / 60)
                    .RotateZ(cookIntensity * GameMath.Sin(capi.World.ElapsedMilliseconds / 50f) / 60)
                    .Translate(origx, 0, origz)
                    .Values
                ;
                prog.ViewMatrix = rpi.CameraMatrixOriginf;
                prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;


                rpi.RenderMultiTextureMesh(lidRef, "tex");
            }

            prog.Stop();
        }

        public void OnUpdate(float temperature)
        {
            temp = temperature;

            float soundIntensity = GameMath.Clamp((temp - 50) / 50, 0, 1);
            SetCookingSoundVolume(isInOutputSlot ? 0 : soundIntensity);
        }

        public void OnCookingComplete()
        {
            isInOutputSlot = true;
        }


        public void SetCookingSoundVolume(float volume)
        {
            if (volume > 0)
            {

                if (cookingSound == null)
                {
                    cookingSound = capi.World.LoadSound(new SoundParams()
                    {
                        Location = new AssetLocation("sounds/effect/cooking.ogg"),
                        ShouldLoop = true,
                        Position = pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                        DisposeOnFinish = false,
                        Range = 10f,
                        ReferenceDistance = 3f,
                        Volume = volume
                    });
                    cookingSound.Start();
                }
                else
                {
                    cookingSound.SetVolume(volume);
                }

            }
            else
            {
                if (cookingSound != null)
                {
                    cookingSound.Stop();
                    cookingSound.Dispose();
                    cookingSound = null;
                }
            }
        }
    }
}
