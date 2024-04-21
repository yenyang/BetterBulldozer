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
        /// A reference to the prefab entity that was removed and is no longer a subelement.
        /// </summary>
        public Entity m_PrefabEntity;

        /// <summary>
        /// Initializes a new instance of the <see cref="PermanentlyRemovedSubElementPrefab"/> struct.
        /// </summary>
        /// <param name="prefabEntity">A reference to the prefab entity that was removed and is no longer a subelement.</param>
        public PermanentlyRemovedSubElementPrefab(Entity prefabEntity)
        {
            m_PrefabEntity = prefabEntity;
        }

        /// <inheritdoc/>
        public bool Equals(PermanentlyRemovedSubElementPrefab other)
        {
            return m_PrefabEntity.Equals(other.m_PrefabEntity);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return m_PrefabEntity.GetHashCode();
        }

        /// <inheritdoc/>
        public void Serialize<TWriter>(TWriter writer)
            where TWriter : IWriter
        {
            writer.Write(1);
            PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>();
            if (prefabSystem.TryGetPrefab(m_PrefabEntity, out PrefabBase prefabBase))
            {
                writer.Write(prefabBase.GetPrefabID().ToString());
                BetterBulldozerMod.Instance.Logger.Debug($"{nameof(PermanentlyRemovedSubElementPrefab)}.{nameof(Serialize)} {prefabBase.GetPrefabID()}");
            }
            else
            {
                BetterBulldozerMod.Instance.Logger.Warn($"Could not serialized {nameof(PermanentlyRemovedSubElementPrefab)}");
            }
        }

        /// <inheritdoc/>
        public void Deserialize<TReader>(TReader reader)
            where TReader : IReader
        {
            reader.Read(out int version);
            reader.Read(out PrefabID prefabID);
            PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>();
            if (prefabSystem.TryGetPrefab(prefabID, out PrefabBase prefabBase))
            {
                prefabSystem.TryGetEntity(prefabBase, out m_PrefabEntity);
            }
            else
            {
                BetterBulldozerMod.Instance.Logger.Warn($"Could not Deserialize {nameof(PermanentlyRemovedSubElementPrefab)}");
            }
        }
    }
}