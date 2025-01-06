namespace Uzi.Modeling.Runtime
{
    public interface IModelLocator<in TSource, out TTarget>
    {
        public TTarget Locate(TSource source);
    }
}