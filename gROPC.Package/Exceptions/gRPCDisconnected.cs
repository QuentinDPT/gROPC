using System;
using System.Collections.Generic;
using System.Text;

namespace gROPC.Package.Exceptions
{
    public class gRPCDisconnected : Exception
    {
        private string _reason = "";
        public string Reason
        {
            get { return _reason; }
            set { }
        }

        public gRPCDisconnected()
        {

        }

        public gRPCDisconnected(string description) :
            base(String.Format("Server disconnected: {0}", description))
        {
            _reason = description;
        }
    }
}
