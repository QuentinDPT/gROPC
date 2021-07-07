using System;
using System.Collections.Generic;
using System.Text;

namespace gROPC
{
    public class SubscriptionResponse<T>
    {
        public T responseValue;

        public Dictionary<string,string> responsesAssociated;
    }
}
