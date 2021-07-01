using System;
using System.Collections.Generic;
using System.Text;

namespace gROPC.Package.Exceptions
{
    public class OPCUnauthorizedOperation : Exception
    {
        private string _reason = "";
        public string Reason
        {
            get { return _reason; }
            set { }
        }

        public OPCUnauthorizedOperation()
        {

        }

        public OPCUnauthorizedOperation(string description) :
            base(String.Format("Unauthorized operation: please whitelist the node on the server ({0})", description))
        {
            _reason = description;
        }
    }
}
