﻿// Copyright (c) Amer Koleci and contributors.
// Distributed under the MIT license. See the LICENSE file in the project root for more information.

using System;

namespace Vortice.Graphics
{
    /// <summary>
    /// Defines the type of <see cref="GraphicsDeviceFactory"/> to create.
    /// </summary>
    public enum GraphicsBackend
    {
        /// <summary>
        /// Best supported device on running platform.
        /// </summary>
        Default,

        /// <summary>
		/// DirectX 11.1+ backend.
		/// </summary>
		Direct3D11,

        /// <summary>
        /// DirectX 12 backend.
        /// </summary>
        Direct3D12,
    }
}