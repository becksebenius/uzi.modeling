using System;

namespace Uzi.Modeling.Runtime
{
    public interface IModelUpdatedCallbackInvoker
    {
        void InvokeModelUpdatedCallbacks(bool force, Action<Exception, object> onExceptionCallback);
    }
}