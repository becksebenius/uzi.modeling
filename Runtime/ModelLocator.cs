namespace Uzi.Modeling.Runtime
{
    public class ModelLocator<TSource, TTarget> : IModelLocator<TSource, TTarget>
    {
        readonly ModelLocatorDelegate<TSource, TTarget> modelLocator;
        public ModelLocator(ModelLocatorDelegate<TSource, TTarget> modelLocator)
        {
            this.modelLocator = modelLocator;
        }

        public TTarget Locate(TSource source) => modelLocator(source);
    }
}