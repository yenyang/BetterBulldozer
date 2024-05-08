// <copyright file="PrefabIdentity.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Better_Bulldozer.Components
{
    using Colossal.Serialization.Entities;
    using Unity.Entities;

    /// <summary>
    /// A buffer component for tracking entirely removed prefabs from a owner's subelements.
    /// </summary>
    public struct OwnerRecord : IComponentData, IQueryTypeParameter, ISerializable
    {
        /// <summary>
        /// A reference to the asset that had a subelement prefab removed.
        /// </summary>
        public Entity m_Owner;

        /// <summary>
        /// Initializes a new instance of the <see cref="OwnerRecord"/> struct.
        /// </summary>
        /// <param name="owner">A reference to the asset that had a subelement prefab removed.</param>
        public OwnerRecord(Entity owner)
        {
            m_Owner = owner;
        }

        /// <inheritdoc/>
        public void Serialize<TWriter>(TWriter writer)
            where TWriter : IWriter
        {
            writer.Write(1);
            writer.Write(m_Owner);
        }

        /// <inheritdoc/>
        public void Deserialize<TReader>(TReader reader)
            where TReader : IReader
        {
            reader.Read(out int version);
            reader.Read(out m_Owner);
        }
    }
}