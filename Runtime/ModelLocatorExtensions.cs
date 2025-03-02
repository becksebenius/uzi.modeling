namespace Uzi.Modeling.Runtime
{
    public static class ModelLocatorExtensions
    {
        public static IModelLocator<TSource, TTarget> GetSubModel<TSource, TIntermediate,TTarget>(
            this IModelLocator<TSource, TIntermediate> locator,
            IModelLocator<TIntermediate, TTarget> redirect)
            => new RedirectModelLocator<TSource, TIntermediate, TTarget>(locator, redirect);

        public static RedirectToListModelLocator<TSource, TIntermediate, TTarget, TTargetEntry> 
            GetSubModel<TSource, TIntermediate,TTarget, TTargetEntry>(
                this IModelLocator<TSource, TIntermediate> locator,
                ListModelLocator<TIntermediate, TTarget, TTargetEntry> redirect)
            where TTarget : IModelList<TTargetEntry>
            => new (locator, redirect);
    }
}