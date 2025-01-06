namespace Uzi.Modeling.Runtime
{
    public class RedirectModelLocator<TSource, TIntermediate, TTarget> : IModelLocator<TSource, TTarget>
    {
        readonly IModelLocator<TSource, TIntermediate> first;
        readonly IModelLocator<TIntermediate, TTarget> second;

        internal RedirectModelLocator(
            IModelLocator<TSource, TIntermediate> first,
            IModelLocator<TIntermediate, TTarget> second)
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