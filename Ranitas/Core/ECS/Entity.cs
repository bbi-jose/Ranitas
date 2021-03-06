﻿using System;

namespace Ranitas.Core.ECS
{
    public struct Entity : IEquatable<Entity>
    {
        #region Constants
        public static int MaxIndex { get { return kIDMask; } }
        public static readonly Entity NullEntity = new Entity(0, 0);
        private const int kIDBits = 20;
        private const int kIDMask = (1 << kIDBits) - 1;
        #endregion

        public readonly int mValue;

        internal int Index { get { return mValue & kIDMask; } }
        internal int Version { get { return mValue >> kIDBits; } }

        internal Entity(int index, int version)
        {
            mValue = (index & kIDMask) | (version << kIDBits);
        }

        public static bool operator ==(Entity a, Entity b) => a.mValue == b.mValue;

        public static bool operator !=(Entity a, Entity b) => a.mValue != b.mValue;

        public bool Equals(Entity other) => mValue == other.mValue;

        public override bool Equals(object obj) => (obj is Entity) && Equals((Entity)obj);

        public override int GetHashCode() => (int)mValue;

        public override string ToString()
        {
            return string.Format("Index: {0}, Version: {1}", Index, Version);
        }

        [Obsolete("Entities are not null")]
        public bool Equals(Entity? other) => false;

        [Obsolete("Entities are not null")]
        public static bool operator ==(Entity? a, Entity b) => false;

        [Obsolete("Entities are not null")]
        public static bool operator !=(Entity? a, Entity b) => true;

        [Obsolete("Entities are not null")]
        public static bool operator ==(Entity a, Entity? b) => false;

        [Obsolete("Entities are not null")]
        public static bool operator !=(Entity a, Entity? b) => true;
    }
}
