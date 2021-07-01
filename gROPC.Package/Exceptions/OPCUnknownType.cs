using System;
using System.Collections.Generic;
using System.Text;

namespace gROPC.Package.Exceptions
{
    public class OPCUnknownType : Exception
    {
        private string _reason = "";
        public string Reason {
            get{ return _reason; }
            set{ }
        }

        public OPCUnknownType()
        {

        }

        public OPCUnknownType(string description) :
            base(String.Format("Unknown type: {0}", description))
        {
            _reason = description;
        }
    }
}
