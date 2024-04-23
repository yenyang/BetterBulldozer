// <copyright file="PermanentlyRemovedSubElementPrefab.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Better_Bulldozer.Components
{
    using System;
    using Better_Bulldozer;
    using Colossal.Serialization.Entities;
    using Game.Prefabs;
    using Unity.Entities;

    /// <summary>
    /// A buffer component for tracking entirely removed prefabs from a owner's subelements.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct PermanentlyRemovedSubElementPrefab : IBufferElementData, IQueryTypeParameter, IEquatable<PermanentlyRemovedSubElementPrefab>, ISerializable
    {
        /// <summary>
        /// A reference to an entity used to record owner and prefabRef.
        /// </summary>
        public Entity m_RecordEntity;

        /// <summary>
        /// Initializes a new instance of the <see cref="PermanentlyRemovedSubElementPrefab"/> struct.
        /// </summary>
        /// <param name="prefabEntity">A reference to an entity used to record owner and prefabRef.</param>
        public PermanentlyRemovedSubElementPrefab(Entity prefabEntity)
        {
            m_RecordEntity = prefabEntity;
        }

        /// <inheritdoc/>
        public bool Equals(PermanentlyRemovedSubElementPrefab other)
        {
            return m_RecordEntity.Equals(other.m_RecordEntity);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return m_RecordEntity.GetHashCode();
        }

        /// <inheritdoc/>
        public void Serialize<TWriter>(TWriter writer)
            where TWriter : IWriter
        {
            writer.Write(1);
            writer.Write( m_RecordEntity );
        }

        /// <inheritdoc/>
        public void Deserialize<TReader>(TReader reader)
            where TReader : IReader
        {
            reader.Read(out int version);
            reader.Read(out m_RecordEntity);
        }
    }
}