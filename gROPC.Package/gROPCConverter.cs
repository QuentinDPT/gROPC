using gROPC.Package.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace gROPC.Package
{
    public class gROPCConverter
    {
        public static T ConvertType<T>(string value) where T : IConvertible
        {
            switch (typeof(T))
            {
                case Type intType when intType == typeof(int):
                    return (T)(object)int.Parse(value);

                case Type intType when intType == typeof(double):
                    return (T)(object)double.Parse(value);

                case Type intType when intType == typeof(float):
                    return (T)(object)float.Parse(value);

                case Type intType when intType == typeof(bool):
                    return (T)(object)bool.Parse(value);

                case Type intType when intType == typeof(string):
                    return (T)(object)value;

                default:
                    throw new OPCUnsupportedType(value.GetType().Name);
            }
        }
    }
}
