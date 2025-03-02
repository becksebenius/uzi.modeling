namespace Uzi.Modeling.Runtime
{
    public interface IModelInternal
    {
        void NotifyChildChanged();
        void ForceMarkDirty();
    }
}