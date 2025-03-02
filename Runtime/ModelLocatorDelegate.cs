namespace Uzi.Modeling.Runtime
{
    public delegate TModel ModelLocatorDelegate<in TModelRoot, out TModel>(TModelRoot source);
}