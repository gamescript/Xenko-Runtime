// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
using SiliconStudio.Core;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Rendering.Materials.ComputeColors;
using SiliconStudio.Xenko.Shaders;

namespace SiliconStudio.Xenko.Rendering.Materials
{
    /// <summary>
    /// A transparent blend material.
    /// </summary>
    [DataContract("MaterialTransparencyBlendFeature")]
    [Display("Blend")]
    public class MaterialTransparencyBlendFeature : MaterialFeature, IMaterialTransparencyFeature
    {
        public const int ShadingColorAlphaFinalCallbackOrder = MaterialGeneratorContext.DefaultFinalCallbackOrder;

        private static readonly MaterialStreamDescriptor AlphaBlendStream = new MaterialStreamDescriptor("DiffuseSpecularAlphaBlend", "matDiffuseSpecularAlphaBlend", MaterialKeys.DiffuseSpecularAlphaBlendValue.PropertyType);

        private static readonly MaterialStreamDescriptor AlphaBlendColorStream = new MaterialStreamDescriptor("DiffuseSpecularAlphaBlend - Color", "matAlphaBlendColor", MaterialKeys.AlphaBlendColorValue.PropertyType);

        private static readonly PropertyKey<bool> HasFinalCallback = new PropertyKey<bool>("MaterialTransparencyAdditiveFeature.HasFinalCallback", typeof(MaterialTransparencyAdditiveFeature));
    
        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialTransparencyBlendFeature"/> class.
        /// </summary>
        public MaterialTransparencyBlendFeature()
        {
            Alpha = new ComputeFloat(1f);
            Tint = new ComputeColor(Color.White);
        }
    
        /// <summary>
        /// Gets or sets the alpha.
        /// </summary>
        /// <value>The alpha.</value>
        /// <userdoc>An additional factor that can be used to modulate original alpha of the material.</userdoc>
        [NotNull]
        [DataMember(10)]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1, 2)]
        public IComputeScalar Alpha { get; set; }

        /// <summary>
        /// Gets or sets the tint color.
        /// </summary>
        /// <value>The tint.</value>
        /// <userdoc>The tint color to apply on the material during the blend.</userdoc>
        [NotNull]
        [DataMember(20)]
        public IComputeColor Tint { get; set; }

        public override void GenerateShader(MaterialGeneratorContext context)
        {
            var alpha = Alpha ?? new ComputeFloat(1f);
            var tint = Tint ?? new ComputeColor(Color.White);

            alpha.ClampFloat(0, 1);

            // Use pre-multiplied alpha to support both additive and alpha blending
            if (context.MaterialPass.BlendState == null)
                context.MaterialPass.BlendState = BlendStates.AlphaBlend;
            context.MaterialPass.HasTransparency = true;
            // TODO GRAPHICS REFACTOR
            //context.Parameters.SetResourceSlow(Effect.BlendStateKey, BlendState.NewFake(blendDesc));

            context.SetStream(AlphaBlendStream.Stream, alpha, MaterialKeys.DiffuseSpecularAlphaBlendMap, MaterialKeys.DiffuseSpecularAlphaBlendValue, Color.White);
            context.SetStream(AlphaBlendColorStream.Stream, tint, MaterialKeys.AlphaBlendColorMap, MaterialKeys.AlphaBlendColorValue, Color.White);
    
            if (!context.Tags.Get(HasFinalCallback))
            {
                context.Tags.Set(HasFinalCallback, true);
                context.AddFinalCallback(MaterialShaderStage.Pixel, AddDiffuseSpecularAlphaBlendColor, ShadingColorAlphaFinalCallbackOrder);
            }
        }
    
        private void AddDiffuseSpecularAlphaBlendColor(MaterialShaderStage stage, MaterialGeneratorContext context)
        {
            context.AddShaderSource(MaterialShaderStage.Pixel, new ShaderClassSource("MaterialSurfaceDiffuseSpecularAlphaBlendColor"));
        }
    }
}
