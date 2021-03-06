﻿// Copyright (c) Amer Koleci and contributors.
// Distributed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Vortice.Graphics
{
    /// <summary>
    /// Defines a graphics device class.
    /// </summary>
    public abstract class GraphicsDevice : DisposableBase
    {
        private readonly object _resourceSyncRoot = new object();
        private readonly List<GraphicsResource> _resources = new List<GraphicsResource>();

        /// <summary>
        /// Gets the device <see cref="GraphicsBackend"/>.
        /// </summary>
        public GraphicsBackend Backend { get; }

        /// <summary>
        /// Gets value indicating gpu validation enable state.
        /// </summary>
        public bool Validation { get; protected set; }

        /// <summary>
        /// Gets the main swap chain <see cref="PresentationParameters"/>.
        /// </summary>
        public PresentationParameters PresentationParameters { get; }

        /// <summary>
        /// Gets the features of this device.
        /// </summary>
        public GraphicsDeviceFeatures Features { get; }

        /// <summary>
        /// Gets the main <see cref="Swapchain"/> created with device.
        /// </summary>
        public abstract Swapchain MainSwapchain { get; }

        /// <summary>
        /// Gets the graphics <see cref="CommandQueue"/>
        /// </summary>
        public abstract CommandQueue GraphicsQueue { get; }

        /// <summary>
        /// Create new instance of <see cref="GraphicsDevice"/> class.
        /// </summary>
        /// <param name="backend"></param>
        /// <param name="presentationParameters"></param>
        protected GraphicsDevice(GraphicsBackend backend, PresentationParameters presentationParameters)
        {
            Guard.IsTrue(backend != GraphicsBackend.Default, nameof(backend), "Invalid backend");
            Guard.NotNull(presentationParameters, nameof(presentationParameters));

            Backend = backend;
            PresentationParameters = presentationParameters;
            Features = new GraphicsDeviceFeatures();
        }

        /// <inheritdoc/>
        protected sealed override void Dispose(bool disposing)
        {
            if (disposing
                && !IsDisposed)
            {
                DestroyAllResources();
                Destroy();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Check if given <see cref="GraphicsBackend"/> is supported.
        /// </summary>
        /// <param name="backend">The <see cref="GraphicsBackend"/> to check.</param>
        /// <returns>True if supported, false otherwise.</returns>
        public static bool IsSupported(GraphicsBackend backend)
        {
            if (backend == GraphicsBackend.Default)
            {
                backend = GetDefaultGraphicsPlatform(Platform.PlatformType);
            }

            switch (backend)
            {
                case GraphicsBackend.Direct3D11:
#if !VORTICE_NO_D3D11
                    return D3D11.D3D11GraphicsDevice.IsSupported();
#else
                    return false;
#endif

                case GraphicsBackend.Direct3D12:
#if !VORTICE_NO_D3D12
                    return D3D12.D3D12GraphicsDevice.IsSupported();
#else
                    return false;
#endif

                case GraphicsBackend.Vulkan:
#if !VORTICE_NO_VULKAN
                    return Vulkan.VulkanGraphicsDevice.IsSupported();
#else
                    return false;
#endif

                default:
                    return false;
            }
        }

        /// <summary>
        /// Create new instance of <see cref="GraphicsDevice"/>
        /// </summary>
        /// <param name="backend">The type of <see cref="GraphicsBackend"/></param>
        /// <param name="validation">Whether to enable validation if supported.</param>
        /// <param name="presentationParameters">The main swap chain parameters.</param>
        /// <returns>New instance of <see cref="GraphicsDevice"/>.</returns>
        public static GraphicsDevice Create(GraphicsBackend backend, bool validation, PresentationParameters presentationParameters)
        {
            Guard.NotNull(presentationParameters, nameof(presentationParameters));

            if (backend == GraphicsBackend.Default)
            {
                backend = GetDefaultGraphicsPlatform(Platform.PlatformType);
            }

            if (!IsSupported(backend))
            {
                throw new GraphicsException($"Backend {backend} is not supported");
            }

            switch (backend)
            {
                case GraphicsBackend.Direct3D11:
#if !VORTICE_NO_D3D11
                    return new D3D11.D3D11GraphicsDevice(validation, presentationParameters);
#else
                    throw new GraphicsException($"{GraphicsBackend.Direct3D11} Backend is not supported");
#endif

                case GraphicsBackend.Direct3D12:
#if !VORTICE_NO_D3D12
                    return new D3D12.D3D12GraphicsDevice(validation, presentationParameters);
#else
                    throw new GraphicsException($"{GraphicsBackend.Direct3D12} Backend is not supported");
#endif

                case GraphicsBackend.Vulkan:
#if !VORTICE_NO_D3D12
                    return new Vulkan.VulkanGraphicsDevice(validation, presentationParameters);
#else
                    throw new GraphicsException($"{GraphicsBackend.Vulkan} Backend is not supported");
#endif

                default:
                    throw new GraphicsException($"Invalid {backend} backend");
            }
        }

        /// <summary>
        /// Gets the best <see cref="GraphicsBackend"/> for given platform.
        /// </summary>
        /// <param name="platformType">The <see cref="PlatformType"/> to detect.</param>
        /// <returns></returns>
        private static GraphicsBackend GetDefaultGraphicsPlatform(PlatformType platformType)
        {
            switch (platformType)
            {
                case PlatformType.Windows:
                case PlatformType.UWP:
                    //if (D3D12.D3D12GraphicsDevice.IsSupported())
                    //{
                    //    return GraphicsBackend.Direct3D12;
                    //}

                    return GraphicsBackend.Direct3D11;
                case PlatformType.Android:
                case PlatformType.Linux:
                    return GraphicsBackend.OpenGLES;

                case PlatformType.iOS:
                case PlatformType.macOS:
                    return GraphicsBackend.OpenGL;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Present content to <see cref="MainSwapchain"/>.
        /// </summary>
        public void Present()
        {
            Present(MainSwapchain);
        }

        /// <summary>
        /// Present content to swapchain.
        /// </summary>
        /// <param name="swapchain">The <see cref="Swapchain"/> to present.</param>
        public void Present(Swapchain swapchain)
        {
            Guard.NotNull(swapchain, nameof(swapchain));

            swapchain.Present();
            FrameCore();
        }

        public GraphicsBuffer CreateBuffer(in BufferDescriptor descriptor, IntPtr initialData)
        {
            Guard.IsTrue(descriptor.BufferUsage != BufferUsage.Unknown, nameof(descriptor.Usage), $"BufferUsage cannot be {nameof(BufferUsage.Unknown)}");
            Guard.MustBeGreaterThan(descriptor.SizeInBytes, 0, nameof(descriptor.SizeInBytes));

            if (descriptor.Usage == GraphicsResourceUsage.Immutable
                && initialData == IntPtr.Zero)
            {
                throw new GraphicsException("Immutable buffer needs valid initial data.");
            }

            return CreateBufferCore(descriptor, initialData);
        }

        public GraphicsBuffer CreateBuffer(in BufferDescriptor descriptor) => CreateBuffer(descriptor, IntPtr.Zero);

        public GraphicsBuffer CreateBuffer<T>(BufferDescriptor descriptor, T[] initialData) where T : struct
        {
            Guard.NotNull(initialData, nameof(initialData));
            Guard.MustBeGreaterThan(initialData.Length, 0, nameof(initialData));

            // Calculate size in bytes if not provided.
            if (descriptor.SizeInBytes == 0)
            {
                descriptor.SizeInBytes = Unsafe.SizeOf<T>() * initialData.Length;
            }

            var handle = GCHandle.Alloc(initialData, GCHandleType.Pinned);
            var buffer = CreateBuffer(descriptor, handle.AddrOfPinnedObject());
            handle.Free();
            return buffer;
        }

        /// <summary>
        /// Create a new immutable <see cref="GraphicsBuffer"/>.
        /// </summary>
        /// <typeparam name="T">Type of data</typeparam>
        /// <param name="bufferUsage">The <see cref="BufferUsage"/></param>
        /// <param name="initialData">Valid iniitial data</param>
        /// <returns>New instance of <see cref="GraphicsBuffer"/></returns>
        public GraphicsBuffer CreateBuffer<T>(BufferUsage bufferUsage, T[] initialData) where T : struct
        {
            return CreateBuffer(
                new BufferDescriptor(Unsafe.SizeOf<T>() * initialData.Length, bufferUsage, GraphicsResourceUsage.Immutable),
                initialData);
        }

        public Texture CreateTexture(in TextureDescription description)
        {
            Guard.IsTrue(description.TextureType != TextureType.Unknown, nameof(description), $"TextureType cannot be {nameof(TextureType.Unknown)}");
            Guard.MustBeGreaterThanOrEqualTo(description.Width, 1, nameof(description.Width));
            Guard.MustBeGreaterThanOrEqualTo(description.Height, 1, nameof(description.Height));
            Guard.MustBeGreaterThanOrEqualTo(description.Depth, 1, nameof(description.Depth));

            return CreateTextureCore(description);
        }

        internal void TrackResource(GraphicsResource resource)
        {
            lock (_resourceSyncRoot)
            {
                _resources.Add(resource);
            }
        }

        internal void UntrackResource(GraphicsResource resource)
        {
            lock (_resourceSyncRoot)
            {
                _resources.Remove(resource);
            }
        }

        private void DestroyAllResources()
        {
            if (_resources.Count > 0)
            {
                lock (_resourceSyncRoot)
                {
                    _resources.Sort((x, y) => x.ResourceType.CompareTo(y.ResourceType));
                    var copyResourceData = _resources.ToArray();
                    for (var i = 0; i < copyResourceData.Length; i++)
                    {
                        var gpuResource = copyResourceData[i];
                        gpuResource.Dispose();
                    }
                }
            }
        }

        protected abstract void Destroy();
        protected abstract void FrameCore();

        protected abstract GraphicsBuffer CreateBufferCore(in BufferDescriptor descriptor, IntPtr initialData);
        protected abstract Texture CreateTextureCore(in TextureDescription description);
    }
}
