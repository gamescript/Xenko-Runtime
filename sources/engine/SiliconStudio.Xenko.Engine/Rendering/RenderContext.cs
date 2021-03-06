// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using SiliconStudio.Core;
using SiliconStudio.Xenko.Engine;
using SiliconStudio.Xenko.Games;
using SiliconStudio.Xenko.Graphics;
using ComponentBase = SiliconStudio.Core.ComponentBase;
using IServiceRegistry = SiliconStudio.Core.IServiceRegistry;

namespace SiliconStudio.Xenko.Rendering
{
    /// <summary>
    /// Rendering context.
    /// </summary>
    public sealed class RenderContext : ComponentBase
    {
        private const string SharedImageEffectContextKey = "__SharedRenderContext__";
        private readonly ThreadLocal<RenderDrawContext> threadContext;
        private readonly object threadContextLock = new object();

        // Used for API that don't support multiple command lists
        internal CommandList SharedCommandList;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderContext" /> class.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <exception cref="System.ArgumentNullException">services</exception>
        internal RenderContext(IServiceRegistry services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            Services = services;
            Effects = services.GetSafeServiceAs<EffectSystem>();
            GraphicsDevice = services.GetSafeServiceAs<IGraphicsDeviceService>().GraphicsDevice;
            Allocator = services.GetServiceAs<GraphicsContext>().Allocator ?? new GraphicsResourceAllocator(GraphicsDevice).DisposeBy(GraphicsDevice);

            threadContext = new ThreadLocal<RenderDrawContext>(() =>
            {
                lock (threadContextLock)
                {
                    return new RenderDrawContext(Services, this, new GraphicsContext(GraphicsDevice, Allocator));
                }
            }, true);
        }

        /// <summary>
        /// Occurs when a renderer is initialized.
        /// </summary>
        public event Action<IGraphicsRendererCore> RendererInitialized;

        /// <summary>
        /// Gets the content manager.
        /// </summary>
        /// <value>The content manager.</value>
        public EffectSystem Effects { get; }

        /// <summary>
        /// Gets the graphics device.
        /// </summary>
        /// <value>The graphics device.</value>
        public GraphicsDevice GraphicsDevice { get; }

        /// <summary>
        /// Gets the services registry.
        /// </summary>
        /// <value>The services registry.</value>
        public IServiceRegistry Services { get; }

        /// <summary>
        /// Gets the time.
        /// </summary>
        /// <value>The time.</value>
        public GameTime Time { get; internal set; }

        /// <summary>
        /// Gets the <see cref="GraphicsResource"/> allocator.
        /// </summary>
        /// <value>The allocator.</value>
        public GraphicsResourceAllocator Allocator { get; }

        /// <summary>
        /// The current render system.
        /// </summary>
        public RenderSystem RenderSystem { get; set; }

        /// <summary>
        /// The current scene instance.
        /// </summary>
        public SceneInstance SceneInstance { get; set; }

        /// <summary>
        /// The current visibility group from the <see cref="SceneInstance"/> and <see cref="RenderSystem"/>.
        /// </summary>
        public VisibilityGroup VisibilityGroup { get; set; }

        /// <summary>
        /// The current render output format (used during the collect phase).
        /// </summary>
        public RenderOutputDescription RenderOutput;

        /// <summary>
        /// The current render output format (used during the collect phase).
        /// </summary>
        public ViewportState ViewportState;

        /// <summary>
        /// The current render view.
        /// </summary>
        public RenderView RenderView { get; set; }

        protected override void Destroy()
        {
            foreach (var renderDrawContext in threadContext.Values)
            {
                renderDrawContext.Dispose();
            }
            threadContext.Dispose();

            base.Destroy();
        }

        /// <summary>
        /// Gets a global shared context.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns>RenderContext.</returns>
        public static RenderContext GetShared(IServiceRegistry services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            // Store RenderContext shared into the GraphicsDevice
            var graphicsDevice = services.GetSafeServiceAs<IGraphicsDeviceService>().GraphicsDevice;
            return graphicsDevice.GetOrCreateSharedData(GraphicsDeviceSharedDataType.PerDevice, SharedImageEffectContextKey, d => new RenderContext(services));
        }

        /// <summary>
        /// Saves a viewport state and restores after using it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>PropertyTagRestore&lt;T&gt;.</returns>
        public ViewportRestore SaveViewportAndRestore()
        {
            return new ViewportRestore(this);
        }

        /// <summary>
        /// Saves a viewport and restores it after using it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>PropertyTagRestore&lt;T&gt;.</returns>
        public RenderOutputRestore SaveRenderOutputAndRestore()
        {
            return new RenderOutputRestore(this);
        }

        /// <summary>
        /// Pushes a render view and restores it after using it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>PropertyTagRestore&lt;T&gt;.</returns>
        public RenderViewRestore PushRenderViewAndRestore(RenderView renderView)
        {
            var result = new RenderViewRestore(this);
            RenderView = renderView;
            return result;
        }

        public RenderDrawContext GetThreadContext() => threadContext.Value;

        public void Reset()
        {
            foreach (var context in threadContext.Values)
            {
                context.ResourceGroupAllocator.Reset(context.CommandList);
            }
        }

        public void Flush()
        {
            foreach (var context in threadContext.Values)
            {
                context.ResourceGroupAllocator.Flush();
                context.QueryManager.Flush();
            }
        }

        internal void OnRendererInitialized(IGraphicsRendererCore obj)
        {
            RendererInitialized?.Invoke(obj);
        }

        public struct ViewportRestore : IDisposable
        {
            private readonly RenderContext context;
            private readonly ViewportState previousValue;

            public ViewportRestore(RenderContext context)
            {
                this.context = context;
                this.previousValue = context.ViewportState;
            }

            public void Dispose()
            {
                context.ViewportState = previousValue;
            }
        }

        public struct RenderOutputRestore : IDisposable
        {
            private readonly RenderContext context;
            private readonly RenderOutputDescription previousValue;

            public RenderOutputRestore(RenderContext context)
            {
                this.context = context;
                this.previousValue = context.RenderOutput;
            }

            public void Dispose()
            {
                context.RenderOutput = previousValue;
            }
        }

        public struct RenderViewRestore : IDisposable
        {
            private readonly RenderContext context;
            private readonly RenderView previousValue;

            public RenderViewRestore(RenderContext context)
            {
                this.context = context;
                this.previousValue = context.RenderView;
            }

            public void Dispose()
            {
                context.RenderView = previousValue;
            }
        }
    }
    public class ViewportState
    {
        public Viewport Viewport0;
        //public Viewport Viewport1;
        //public Viewport Viewport2;
        //public Viewport Viewport3;
        //public Viewport Viewport4;
        //public Viewport Viewport5;
        //public Viewport Viewport6;
        //public Viewport Viewport7;

        /// <summary>
        /// Capture state from <see cref="CommandList.Viewports"/>.
        /// </summary>
        /// <param name="commandList">The command list to capture state from.</param>
        public unsafe void CaptureState(CommandList commandList)
        {
            // TODO: Support multiple viewports
            var viewports = commandList.Viewports;
            fixed (Viewport* viewport0 = &Viewport0)
            {
                for (int i = 0; i < 1; ++i)
                {
                    viewport0[i] = viewports[i];
                }
            }
        }
    }
}
