namespace Uzi.Modeling.Runtime
{
    public interface IModelCache<out T>
    {
        T Value { get; }
    }
}