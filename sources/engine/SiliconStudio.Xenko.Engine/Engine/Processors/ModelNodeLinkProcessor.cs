// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

using SiliconStudio.Xenko.Rendering;

namespace SiliconStudio.Xenko.Engine.Processors
{
    public class ModelNodeLinkProcessor : EntityProcessor<ModelNodeLinkComponent>
    {
        public ModelNodeLinkProcessor()
            : base(typeof(TransformComponent))
        {
        }

        protected override ModelNodeLinkComponent GenerateComponentData(Entity entity, ModelNodeLinkComponent component)
        {
            return component;
        }
        
        protected override void OnEntityComponentAdding(Entity entity, ModelNodeLinkComponent component, ModelNodeLinkComponent data)
        {
            //populate the valid property
            component.ValidityCheck();

            entity.EntityManager.HierarchyChanged += component.OnHierarchyChanged;
        }

        protected override void OnEntityComponentRemoved(Entity entity, ModelNodeLinkComponent component, ModelNodeLinkComponent data)
        {
            // Reset TransformLink
            if (entity.Transform.TransformLink is ModelNodeTransformLink)
                entity.Transform.TransformLink = null;

            entity.EntityManager.HierarchyChanged -= component.OnHierarchyChanged;
        }

        public override void Draw(RenderContext context)
        {
            foreach (var item in ComponentDatas)
            {           
                var entity = item.Key.Entity;              
                var transformComponent = entity.Transform;
                
                if (item.Value.IsValid)
                {
                    var modelNodeLink = item.Value;
                    var transformLink = transformComponent.TransformLink as ModelNodeTransformLink;

                    // Try to use Target, otherwise Parent
                    var modelComponent = modelNodeLink.Target;
                    var modelEntity = modelComponent?.Entity ?? transformComponent.Parent?.Entity;

                    // Check against Entity instead of ModelComponent to avoid having to get ModelComponent when nothing changed)
                    if (transformLink == null || transformLink.NeedsRecreate(modelEntity, modelNodeLink.NodeName))
                    {
                        // In case we use parent, modelComponent still needs to be resolved
                        if (modelComponent == null)
                            modelComponent = modelEntity?.Get<ModelComponent>();

                        // If model component is not parent, we want to use forceRecursive because we might want to update this link before the modelComponent.Entity is updated (depending on order of transformation update)
                        transformComponent.TransformLink = modelComponent != null
                            ? new ModelNodeTransformLink(modelComponent, modelNodeLink.NodeName, modelEntity != transformComponent.Parent?.Entity)
                            : null;
                    }
                }
                else
                {
                    transformComponent.TransformLink = null;
                }
            }
        }
    }
}
