namespace Uzi.Modeling.Runtime
{
    public interface IModel<out T>
    {
        void RegisterObserver(IModelObserver<T> observer);
        void DeregisterObserver(IModelObserver<T> observer);
    }
}