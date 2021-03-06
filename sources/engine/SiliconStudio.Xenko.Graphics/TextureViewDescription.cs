// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
namespace SiliconStudio.Xenko.Graphics
{
    /// <summary>
    /// View description of a <see cref="Texture"/>.
    /// </summary>
    public struct TextureViewDescription
    {
        /// <summary>
        /// The flags used for the view. If <see cref="TextureFlags.None"/> then the view is using the flags from the texture.
        /// </summary>
        public TextureFlags Flags;

        /// <summary>
        /// The format of the view (used for the ShaderResource or Unordered access).
        /// </summary>
        public PixelFormat Format;

        /// <summary>
        /// The <see cref="ViewType"/> (single mip, band, or full)
        /// </summary>
        public ViewType Type;

        /// <summary>
        /// The array slice index.
        /// </summary>
        public int ArraySlice;

        /// <summary>
        /// The mip level index.
        /// </summary>
        public int MipLevel;

        /// <summary>
        /// Gets a staging compatible description of this instance.
        /// </summary>
        /// <returns>TextureViewDescription.</returns>
        public TextureViewDescription ToStagingDescription()
        {
            var viewDescription = this;
            viewDescription.Flags = TextureFlags.None;
            return viewDescription;
        }
    }
}
