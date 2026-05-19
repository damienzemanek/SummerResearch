using System;
using EMILtools_Private.Core;

namespace EMILtools_Private.Core
{
    [Serializable]
    public struct BoolInt
    {
        public bool boolVal;
        public int intVal;
    }
    
    public class SignalSenderTcBoolInt : SignalSenderTC<BoolInt>
    {
        
    }
}
