﻿using System.Diagnostics;

namespace Ranitas.Core.ECS
{
    public sealed class SliceEntityOutput
    {
        private RestrictedArray<Entity> mArray = null;

        public uint Count
        {
            get
            {
                Debug.Assert(mArray != null, "This output has not been linked to an entity slice.");
                return mArray.Count;
            }
        }

        public Entity this[uint index]
        {
            get
            {
                Debug.Assert(mArray != null, "This output has not been linked to an entity slice.");
                return mArray[index];
            }
        }
    }
}
