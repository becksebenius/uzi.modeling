namespace Uzi.Modeling.Runtime
{
    public interface IListModelLocator<in TSource, out TTarget, TTargetEntry> 
        : IModelLocator<TSource, TTarget>
        where TTarget : IModelList<TTargetEntry>
    {
    }
}