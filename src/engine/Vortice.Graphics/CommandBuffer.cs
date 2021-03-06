﻿// Copyright (c) Amer Koleci and contributors.
// Distributed under the MIT license. See the LICENSE file in the project root for more information.

using System;

namespace Vortice.Graphics
{
    /// <summary>
    /// Defines a command buffer for storing recorded graphics commands.
    /// </summary>
    public abstract class CommandBuffer : DisposableBase
    {
        /// <summary>
        /// Gets the creation <see cref="CommandQueue"/>.
        /// </summary>
        public CommandQueue Queue { get; }

        /// <summary>
        /// Gets the creation <see cref="GraphicsDevice"/>.
        /// </summary>
        public GraphicsDevice Device => Queue.Device;

        /// <summary>
        /// Gets or sets the execution order.
        /// </summary>
        public int ExecutionOrder { get; set; }

        /// <summary>
        /// Create a new instance of <see cref="CommandBuffer"/> class.
        /// </summary>
        /// <param name="queue">The creation queue</param>
        protected CommandBuffer(CommandQueue queue)
        {
            Queue = queue;
        }

        /// <inheritdoc/>
        protected sealed override void Dispose(bool disposing)
        {
            if (disposing
                && !IsDisposed)
            {
                //DestroyAllResources();
                Destroy();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Begin rendering with main swap chain render pass.
        /// </summary>
        public void BeginRenderPass()
        {
            BeginRenderPass(Device.MainSwapchain.CurrentRenderPassDescriptor);
        }

        /// <summary>
        /// Begin rendering with given descriptor.
        /// </summary>
        /// <param name="descriptor">The <see cref="RenderPassDescriptor"/></param>
        public void BeginRenderPass(RenderPassDescriptor descriptor)
        {
            Guard.NotNull(descriptor, nameof(descriptor));

            BeginRenderPassCore(descriptor);
        }

        public void EndRenderPass()
        {
            EndRenderPassCore();
        }

        public void Commit()
        {
            CommitCore();
        }

        protected abstract void Destroy();
        protected abstract void BeginRenderPassCore(RenderPassDescriptor descriptor);
        protected abstract void EndRenderPassCore();
        protected abstract void CommitCore();
    }
}
