namespace Uzi.Modeling.Runtime
{
    public interface IModelObserver<in T>
    {
        void OnModelUpdated(T value, ModelObservationFlags flags);
    }
}