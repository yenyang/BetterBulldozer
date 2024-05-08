// <copyright file="DeleteInXFrames.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>
namespace Better_Bulldozer.Components
{
    using Unity.Entities;

    /// <summary>
    /// A component used to delete an entity on the next frame.
    /// </summary>
    public struct DeleteInXFrames : IComponentData, IQueryTypeParameter
    {
        /// <summary>
        /// A count for frames remaining until deletion.
        /// </summary>
        public int m_FramesRemaining;
    }
}