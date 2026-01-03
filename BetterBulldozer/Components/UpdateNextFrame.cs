// <copyright file="UpdateNextFrame.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>
namespace Better_Bulldozer.Components
{
    using Unity.Entities;

    /// <summary>
    /// A component used to delete an entity on the next frame.
    /// </summary>
    public struct UpdateNextFrame : IComponentData, IQueryTypeParameter
    {
    }
}