using System.Collections.Generic;

namespace Uzi.Modeling.Runtime
{
    public interface IModelList<T> : IReadOnlyList<T>, IModel<IModelList<T>>
    {
        void AddRange(IEnumerable<T> entries);
        T Add();
        void Add(T entry);
        T Insert(int index);
        void Insert(T entry, int index);
        void RemoveAt(int index);
        void Clear();
        void SetCount(int count);
    }
}