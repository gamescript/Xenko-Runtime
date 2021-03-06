// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
namespace SiliconStudio.Xenko.Rendering
{
    /// <summary>
    /// Defines keys associated with mesh used for compiling assets.
    /// </summary>
    public sealed class MaterialAssetKeys
    {
        /// <summary>
        /// When compiling effect with an EffectLibraryAsset (xkfxlib), set it to true to allow permutation based on the 
        /// parameters of all materials.
        /// </summary>
        /// <userdoc>
        /// If checked, the material parameters will be used to generate effects.
        /// </userdoc>
        public static readonly ValueParameterKey<bool> UseParameters = ParameterKeys.NewValue<bool>();

        /// <summary>
        /// Allow material compilation without mesh.
        /// </summary>
        /// <userdoc>
        /// If checked, the materials will generate a shader even if they are not attached to a mesh.
        /// </userdoc>
        public static readonly ValueParameterKey<bool> GenerateShader = ParameterKeys.NewValue<bool>();
    }
}
