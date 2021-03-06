// Copyright (c) 2011-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
using System.ComponentModel;
using SiliconStudio.Xenko.Engine;

namespace SiliconStudio.Xenko.Rendering
{
    public class SimpleGroupToRenderStageSelector : RenderStageSelector
    {
        [DefaultValue(RenderGroupMask.Group0)]
        public RenderGroupMask RenderGroup { get; set; } = RenderGroupMask.Group0;

        public RenderStage RenderStage { get; set; }

        public string EffectName { get; set; }

        public override void Process(RenderObject renderObject)
        {
            if (RenderStage != null && ((RenderGroupMask)(1U << (int)renderObject.RenderGroup) & RenderGroup) != 0)
            {
                renderObject.ActiveRenderStages[RenderStage.Index] = new ActiveRenderStage(EffectName);
            }
        }
    }
}
