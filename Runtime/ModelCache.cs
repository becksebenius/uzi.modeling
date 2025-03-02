namespace Uzi.Modeling.Runtime
{
    public class ModelCache<T> : IModelCache<T>
    {
        public T Value { get; set; }
    }
}