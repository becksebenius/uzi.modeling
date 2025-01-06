namespace Uzi.Modeling.Runtime
{
    public class RedirectToListModelLocator<TSource, TIntermediate, TTarget, TTargetEntry> : IListModelLocator<TSource, TTarget, TTargetEntry>
        where TTarget : IModelList<TTargetEntry>
    {
        readonly IModelLocator<TSource, TIntermediate> first;
        readonly ListModelLocator<TIntermediate, TTarget, TTargetEntry> second;

        internal RedirectToListModelLocator(
            IModelLocator<TSource, TIntermediate> first,
            ListModelLocator<TIntermediate, TTarget, TTargetEntry> second)
        {
            this.first = first;
            this.second = second;
        }

        public TTarget Locate(TSource source)
        {
            return second.Locate(first.Locate(source));
        }
    }
}