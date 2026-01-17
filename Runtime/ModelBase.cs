using System;
using System.Collections.Generic;
using Uzi;
using Uzi.Serialization;

namespace Uzi.Modeling.Runtime
{
    public static class ModelBaseUtility
    {
        public static readonly Action<Exception, object> DefaultExceptionCallback = (e, _) => Logging.Exception(LogCategory.UI, e);
    }
    
    [JSONSerializeNonPublic]
    public abstract class ModelBase<T> : IModel<T>, IModelInternal, IModelUpdatedCallbackInvoker, IJSONCustomDeserialization
        where T : ModelBase<T>
    {
        // Note: marked public because owners need to be able to set this
        //  when constructing an instance or when inheriting an instance
        //  and don't want to put in the constructor so that client code can
        //  use a parameterless constructor to create model data
        [JSONIgnore] public IModelInternal parent;

        [JSONIgnore] int selfChangeId;
        [JSONIgnore] int childChangeId;
        [JSONIgnore] int lastReportedSelfChangeId;
        [JSONIgnore] int lastReportedChildChangeId;
        [JSONIgnore] List<IModelObserver<T>> observers;
        [JSONIgnore] string typeNameCache;
        string TypeName => typeNameCache ??= GetType().Name;

        void IModel<T>.RegisterObserver(IModelObserver<T> observer)
        {
            observers ??= new();
            observers.Add(observer);
            observer.OnModelUpdated((T)this, ModelObservationFlags.All);
        }

        void IModel<T>.DeregisterObserver(IModelObserver<T> observer)
        {
            if(observers == null)
            {
                return;
            }
            observers.Remove(observer);
        }

        protected void SelfChanged()
        {
            ++selfChangeId;
        }
        
        void IModelInternal.NotifyChildChanged()
        {
            ++childChangeId;
            parent?.NotifyChildChanged();
        }

        public void TransferInternals(T other)
        {
            other.observers = observers;
            other.selfChangeId = selfChangeId+1;
            other.lastReportedSelfChangeId = lastReportedSelfChangeId;
            other.childChangeId = childChangeId;
            other.lastReportedChildChangeId = lastReportedChildChangeId;
            other.parent = parent;
            other.ForceMarkDirty();

            observers = default;
            parent = default;
        }

        void IModelInternal.ForceMarkDirty() => ForceMarkDirty();
        protected virtual void ForceMarkDirty()
        {
            ++selfChangeId;
        }

        void IModelUpdatedCallbackInvoker.InvokeModelUpdatedCallbacks(bool force, Action<Exception, object> onExceptionCallback)
            => InvokeModelUpdatedCallbacks(force, onExceptionCallback);
        public void InvokeModelUpdatedCallbacks(bool force, Action<Exception, object> onExceptionCallback = null)
        {
            onExceptionCallback ??= ModelBaseUtility.DefaultExceptionCallback;
            
            if(!force 
            && selfChangeId == lastReportedSelfChangeId
            && childChangeId == lastReportedChildChangeId)
            {
                return;
            }

            InvokeModelUpdatedCallbacksOnChildren(force, onExceptionCallback);
            
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

                UnityEngine.Profiling.Profiler.BeginSample(TypeName);
                for(int i = 0; i < observers.Count; ++i)
                {
                    var observer = observers[i];
                    try
                    {
                        observer.OnModelUpdated((T)this, flags);
                    }
                    catch(Exception e)
                    {
                        onExceptionCallback?.Invoke(e, observer);
                    }
                }
                UnityEngine.Profiling.Profiler.EndSample();
            }
            lastReportedChildChangeId = childChangeId;
            lastReportedSelfChangeId = selfChangeId;
        }
        protected virtual void InvokeModelUpdatedCallbacksOnChildren(bool force, Action<Exception, object> onExceptionCallback) {}

        void IJSONCustomDeserialization.DeserializeField(ref JSONDeserializer deserializer, string fieldName)
            => DeserializeField(ref deserializer, fieldName);
        protected virtual void DeserializeField(ref JSONDeserializer deserializer, string fieldName) {}
    }
}