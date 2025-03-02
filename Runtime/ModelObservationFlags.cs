using System;

namespace Uzi.Modeling.Runtime
{
    [Flags]
    public enum ModelObservationFlags
    {
        Self = 1 << 0,
        Children = 1 << 1,
        All = ~0
    }
}