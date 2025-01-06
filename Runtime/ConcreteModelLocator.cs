namespace Uzi.Modeling.Runtime
{
    public delegate T ConcreteModelLocator<out T>() where T : IModel<T>;
}