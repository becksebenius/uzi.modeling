namespace Uzi.Modeling.Runtime
{
    public class ListModelLocator<TSource, TTarget, TTargetEntry> : IListModelLocator<TSource, TTarget, TTargetEntry>
        where TTarget : IModelList<TTargetEntry>
    {
        readonly ModelLocatorDelegate<TSource, TTarget> modelLocator;

        public ListModelLocator(ModelLocatorDelegate<TSource, TTarget> modelLocator)
        {
            this.modelLocator = modelLocator;
        }

        public TTarget Locate(TSource source) => modelLocator(source);
    }
}