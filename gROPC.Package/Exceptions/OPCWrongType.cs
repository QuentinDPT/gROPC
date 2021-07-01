using System;
using System.Collections.Generic;
using System.Text;

namespace gROPC.Package.Exceptions
{
    public class OPCWrongType : Exception
    {
        private string _reason = "";
        public string Reason
        {
            get { return _reason; }
            set { }
        }

        public OPCWrongType(){

        }

        public OPCWrongType(string description) :
            base(String.Format("Wrong type: another type is assigned to this node ({0})", description))
        {
            _reason = description;
        }
    }
}
