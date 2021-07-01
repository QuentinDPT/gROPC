using System;
using System.Collections.Generic;
using System.Text;

namespace gROPC.Package.Exceptions
{
    class OPCUnsupportedType : Exception
    {
        private string _reason = "";
        public string Reason
        {
            get { return _reason; }
            set { }
        }

        public OPCUnsupportedType()
        {

        }

        public OPCUnsupportedType(string description) :
            base(String.Format("Unsupported type: {0}", description))
        {
            _reason = description;
        }
    }
}
