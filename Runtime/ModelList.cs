using System;
using System.Collections;
using System.Collections.Generic;
using Uzi.Serialization;

namespace Uzi.Modeling.Runtime
{
    [JSONSerializeNonPublic]
    public class ModelList<T> : IModelList<T>, IModelInternal
        where T : ModelBase<T>, new()
    {
        [JSONIgnore]
        readonly IModelInternal @internal;
        
        [JSONIgnore]
        readonly Action<T, IModelInternal> assignParentMethod;
        
        readonly List<T> entries;
        
        public ModelList(IModelInternal @internal, Action<T, IModelInternal> assignParentMethod)
        {
            this.@internal = @internal;
            this.assignParentMethod = assignParentMethod;
            entries = new List<T>();
        }

        public void AddRange(IEnumerable<T> collection)
        {
            foreach (var value in collection)
            {
                Add(value);
            }
        }
        public T Add()
        {
            var entry = new T();
            Add(entry);
            return entry;
        }
        public void Add(T entry)
        {
            assignParentMethod(entry, this);
            entries.Add(entry);
            SelfChanged();
        }

        public void RemoveAt(int index)
        {
            entries.RemoveAt(index);
            SelfChanged();
        }

        public T Insert(int index)
        {
            var entry = new T();
            Insert(entry, index);
            return entry;
        }
        public void Insert(T entry, int index)
        {
            assignParentMethod(entry, this);
            entries.Insert(index, entry);
            SelfChanged();
        }

        public void Clear()
        {
            entries.Clear();
            SelfChanged();
        }

        public void SetCount(int count)
        {
            while (Count < count)
            {
                Add(new T());
            }

            while (count < Count)
            {
                RemoveAt(Count-1);
            }
        }

        public void InvokeModelUpdatedCallbacks(bool force, Action<Exception, object> onExceptionCallback)
        {
            if(!force 
               && selfChangeId == lastReportedSelfChangeId
               && childChangeId == lastReportedChildChangeId)
            {
                return;
            }
            
            for (int i = 0; i < entries.Count; ++i)
            {
                entries[i].InvokeModelUpdatedCallbacks(force, onExceptionCallback);
            }
            
            if(observers != null)
            {
                ModelObservationFlags flags = 0;
                if (force)
                {
                    flags = ModelObservationFlags.All;
                }
                else
                {
                    if (lastReportedChildChangeId != childChangeId)
                    {
                        flags |= ModelObservationFlags.Children;
                    }
                    
                    if (lastReportedSelfChangeId != selfChangeId)
                    {
                        flags |= ModelObservationFlags.Self;
                    }
                }
                
                for(int i = 0; i < observers.Count; ++i)
                {
                    var observer = observers[i];
                    try
                    {
                        observer.OnModelUpdated(this, flags);
                    }
                    catch(Exception e)
                    {
                        onExceptionCallback?.Invoke(e, observer);
                    }
                }
            }

            lastReportedChildChangeId = childChangeId;
            lastReportedSelfChangeId = selfChangeId;
        }

        void ChildChanged()
        {
            ++childChangeId;
            @internal.NotifyChildChanged();
        }
        
        void SelfChanged()
        {
            ++selfChangeId;
            @internal.NotifyChildChanged();
        }

        public IEnumerator<T> GetEnumerator() => entries.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public int Count => entries.Count;

        public T this[int index]
        {
            get => entries[index];
            set
            {
                if (Count <= index)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                var current = entries[index];
                current.TransferInternals(value);
                entries[index] = value;
            }
        }
        
        void IModelInternal.NotifyChildChanged() => ChildChanged();

        public static void Deserialize(ref JSONDeserializer deserializer, ModelList<T> target, string fieldName)
        {
            var list = deserializer.ReadValue<List<T>>(fieldName);
            
            target.Clear();
            foreach (T value in list)
            {
                target.assignParentMethod(value, target);
                target.entries.Add(value);
            }
        }

        [JSONIgnore] int selfChangeId;
        [JSONIgnore] int lastReportedSelfChangeId;
        
        [JSONIgnore] int childChangeId;
        [JSONIgnore] int lastReportedChildChangeId;
        
        [JSONIgnore] List<IModelObserver<IModelList<T>>> observers;

        void IModelInternal.ForceMarkDirty() => ForceMarkDirty();
        internal void ForceMarkDirty()
        {
            ++selfChangeId;
            for (int i = 0; i < entries.Count; ++i)
            {
                //entries[i].ForceMarkDirty();
            }
        }

        void IModel<IModelList<T>>.RegisterObserver(IModelObserver<IModelList<T>> observer)
        {
            observers ??= new();
            observers.Add(observer);
            observer.OnModelUpdated(this, ModelObservationFlags.All);
        }

        void IModel<IModelList<T>>.DeregisterObserver(IModelObserver<IModelList<T>> observer)
        {
            if(observers == null)
            {
                return;
            }
            observers.Remove(observer);
        }
    }
}