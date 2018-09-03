﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Ranitas.Core.ECS
{
    public class EntityRegistry
    {
        public EntityRegistry(int maxEntities)
        {
            Debug.Assert(maxEntities <= Entity.MaxIndex, "Can't store this many entities!");
            mEntities = new Entity[maxEntities + 1];
            mEntities[0] = new Entity(0, uint.MaxValue); //Index 0 is never assigned, and NullEntity is invalid.
            mNext = 1;
            for (uint i = 1; i <= maxEntities; ++i)
            {
                mEntities[i] = new Entity(i + 1, 0);    //Point to the next entity available
            }
        }

        public int Capacity
        {
            get { return mEntities.Length - 1; }
        }

        public Entity Create()
        {
            uint createdIndex = mNext;
            Debug.Assert(createdIndex < mEntities.Length, "Entities have run out!");
            Entity inPlaceEntity = mEntities[createdIndex];
            mNext = inPlaceEntity.Index;
            Entity createdEntity = new Entity(createdIndex, inPlaceEntity.Version);
            mEntities[createdIndex] = createdEntity;
            return createdEntity;
        }

        public void Destroy(Entity entity)
        {
            Debug.Assert(IsValid(entity), "Can't destroy invalid entity.");
            uint entityIndex = entity.Index;

            //It is important to clear slices before touching the component sets, as the component sets drive the slices internally!
            foreach (IUntypedComponentSet componentSet in mComponentSets)
            {
                if (componentSet.Contains(entityIndex))
                {
                    componentSet.Remove(entityIndex);
                }
            }
            //Remove from any slices! This is not done through remove value/component events since a slice can be made up of only exclusions
            foreach (EntitySlice slice in mRegisteredSlices)
            {
                slice.RemoveValue(entityIndex);
            }

            mEntities[entityIndex] = new Entity(mNext, entity.Version + 1);
            mNext = entityIndex;
        }

        public bool IsValid(Entity entity)
        {
            return mEntities[entity.Index] == entity;
        }

        public bool HasComponent<TComponent>(Entity entity) where TComponent : struct
        {
            Debug.Assert(IsValid(entity));
            IUntypedComponentSet componentSet = GetUntypedIndexedSet<TComponent>();
            return componentSet.Contains(entity.Index);
        }

        public void AddComponent<TComponent>(Entity entity, TComponent component) where TComponent : struct
        {
            Debug.Assert(IsValid(entity));
            ComponentSet<TComponent> componentSet = GetComponentSet<TComponent>();
            componentSet.Add(component, entity.Index);
        }

        public void SetComponent<TComponent>(Entity entity, TComponent component) where TComponent : struct
        {
            Debug.Assert(IsValid(entity));
            ComponentSet<TComponent> componentSet = GetComponentSet<TComponent>();
            componentSet.Replace(component, entity.Index);
        }

        public void SetOrAddComponent<TComponent>(Entity entity, TComponent component) where TComponent : struct
        {
            Debug.Assert(IsValid(entity));
            ComponentSet<TComponent> componentSet = GetComponentSet<TComponent>();
            componentSet.AddOrReplace(component, entity.Index);
        }

        public TComponent GetComponent<TComponent>(Entity entity) where TComponent : struct
        {
            Debug.Assert(IsValid(entity));
            ComponentSet<TComponent> componentSet = GetComponentSet<TComponent>();
            return componentSet.GetValue(entity.Index);
        }

        public void RemoveComponent<TComponent>(Entity entity) where TComponent : struct
        {
            Debug.Assert(IsValid(entity));
            IUntypedComponentSet componentSet = GetUntypedIndexedSet<TComponent>();
            componentSet.Remove(entity.Index);
        }

        public EntitySliceConfiguration ConfigureSlice()
        {
            EntitySliceConfiguration newSlice = new EntitySliceConfiguration(this);
            return newSlice;
        }

        private void ValidateOrRegisterComponentType<TComponent>() where TComponent : struct
        {
            Type componentType = typeof(TComponent);
            if (!mComponentSetLookup.ContainsKey(componentType))
            {
                ComponentSet<TComponent> componentSet = new ComponentSet<TComponent>(mEntities.Length);
                ushort lookup = (ushort)mComponentSets.Count;
                mComponentSets.Add(componentSet);
                mComponentSetLookup.Add(componentType, lookup);
            }
        }

        private IUntypedComponentSet GetUntypedIndexedSet<TComponent>() where TComponent : struct
        {
            Type componentType = typeof(TComponent);
            ValidateOrRegisterComponentType<TComponent>();
            ushort lookup = mComponentSetLookup[componentType];
            return mComponentSets[lookup];
        }

        private ComponentSet<TComponent> GetComponentSet<TComponent>() where TComponent : struct
        {
            Type componentType = typeof(TComponent);
            ValidateOrRegisterComponentType<TComponent>();
            ushort lookup = mComponentSetLookup[componentType];
            return (ComponentSet<TComponent>)mComponentSets[lookup];
        }

        private uint mNext;
        private Entity[] mEntities;

        private List<IUntypedComponentSet> mComponentSets = new List<IUntypedComponentSet>();
        private Dictionary<Type, ushort> mComponentSetLookup = new Dictionary<Type, ushort>();

        private List<EntitySlice> mRegisteredSlices = new List<EntitySlice>();

        #region Entity slice classes and interfaces
        public class EntitySliceConfiguration
        {
            private EntityRegistry mRegistry;
            List<IPublishingIndexSet> mRequirements = new List<IPublishingIndexSet>();
            List<IValueInjector> mInjectors = new List<IValueInjector>();
            List<IPublishingIndexSet> mExclusions = new List<IPublishingIndexSet>();

            public EntitySliceConfiguration(EntityRegistry registry)
            {
                mRegistry = registry;
            }

            public EntitySliceConfiguration Require<TComponent>(SliceRequirementOutput<TComponent> targetOutput) where TComponent : struct
            {
                Debug.Assert(targetOutput != null, "Target output cannot be null before calling this method!");

                ComponentSet<TComponent> componentSet = mRegistry.GetComponentSet<TComponent>();
                mRequirements.Add(componentSet);

                RestrictedArray<TComponent> writeArray = new RestrictedArray<TComponent>(mRegistry.Capacity);
                ValueInjector<TComponent> injector = new ValueInjector<TComponent>(componentSet, writeArray);
                mInjectors.Add(injector);

                //Set the write array to be accessed by the component output passed
                Type outputType = typeof(SliceRequirementOutput<TComponent>);
                FieldInfo arrayField = outputType.GetField("mArray", BindingFlags.NonPublic | BindingFlags.Instance);
                arrayField.SetValue(targetOutput, writeArray);

                return this;
            }

            public EntitySliceConfiguration Exclude<TComponent>() where TComponent : struct
            {
                ComponentSet<TComponent> componentSet = mRegistry.GetComponentSet<TComponent>();
                mExclusions.Add(componentSet);
                return this;
            }

            public void CreateSlice()
            {
                EntitySlice slice = new EntitySlice(mRegistry.mEntities.Length, mRequirements, mInjectors, mExclusions);
                mRegistry.mRegisteredSlices.Add(slice);
                //Invalidate further use
                mRegistry = null;
                mRequirements.Clear();
                mInjectors.Clear();
                mExclusions.Clear();
            }
        }

        private class EntitySlice
        {
            private readonly FilteredIndexSet mFilteredSet;
            private readonly IValueInjector[] mInjectors;

            public EntitySlice(int capacity, List<IPublishingIndexSet> requirements, List<IValueInjector> injectors, List<IPublishingIndexSet> exclusions)
            {
                Debug.Assert(requirements.Count == injectors.Count);    //Assumption is there is an injector per requirement (which should match the type)

                IReadonlyIndexSet[] reqArray = ArrayCastedType(requirements);
                IReadonlyIndexSet[] exclArray = ArrayCastedType(exclusions);
                mFilteredSet = new FilteredIndexSet(capacity, reqArray, exclArray);

                for (int i = 0; i < requirements.Count; ++i)
                {
                    IPublishingIndexSet publisher = requirements[i];
                    publisher.NewValue += TryAddValue;
                    publisher.Removed += RemoveValue;
                    int injectorIndex = i;
                    publisher.ValueModified += (index) => UpdateValue(index, injectorIndex);
                }

                for (int i = 0; i < exclusions.Count; ++i)
                {
                    IPublishingIndexSet publisher = exclusions[i];
                    publisher.NewValue += RemoveValue;
                    publisher.Removed += TryAddValue;
                }

                mInjectors = injectors.ToArray();
            }

            private static IReadonlyIndexSet[] ArrayCastedType(List<IPublishingIndexSet> items)
            {
                IReadonlyIndexSet[] itemsArray = new IReadonlyIndexSet[items.Count];
                for (int i = 0; i < items.Count; ++i)
                {
                    itemsArray[i] = items[i];
                }
                return itemsArray;
            }

            private void TryAddValue(uint indexID)
            {
                if (mFilteredSet.TryInsert(indexID))
                {
                    foreach (IValueInjector injector in mInjectors)
                    {
                        injector.InjectNewValue(indexID);
                    }
                }
            }

            public void RemoveValue(uint indexID)
            {
                if (mFilteredSet.Remove(indexID))
                {
                    foreach (IValueInjector injector in mInjectors)
                    {
                        injector.RemoveValue(indexID);
                    }
                }
            }

            private void UpdateValue(uint indexID, int injectorIndex)
            {
                if (mFilteredSet.Contains(indexID))
                {
                    mInjectors[injectorIndex].InjectExistingValue(indexID);
                }
            }
        }

        private interface IValueInjector
        {
            void InjectNewValue(uint indexID);
            void InjectExistingValue(uint indexID);
            void RemoveValue(uint indexID);
        }

        private class ValueInjector<TValue> : IValueInjector where TValue : struct
        {
            private IIndexedSet<TValue> mSourceSet;
            private RestrictedArray<TValue> mTargetOutput;

            public ValueInjector(IIndexedSet<TValue> source, RestrictedArray<TValue> target)
            {
                mSourceSet = source;
                mTargetOutput = target;
            }

            public void InjectNewValue(uint indexID)
            {
                TValue value = mSourceSet.GetValue(indexID);
                mTargetOutput.AddValue(value);
            }

            public void InjectExistingValue(uint indexID)
            {
                TValue value = mSourceSet.GetValue(indexID);
                uint packedIndex = mSourceSet.GetPackedIndex(indexID);
                mTargetOutput.SetValue(value, packedIndex);
            }

            public void RemoveValue(uint indexID)
            {
                uint packedIndex = mSourceSet.GetPackedIndex(indexID);
                mTargetOutput.RemoveValue(packedIndex);
            }
        }
        #endregion
    }
}
