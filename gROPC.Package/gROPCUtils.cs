using gROPC.Package.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gROPC
{
    public static class gROPCUtils
    {
        public static T[] SubArray<T>(this T[] data, int index, int length = -1)
        {
            if (length == -1)
                length = data.Length - index;

            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public static T ConvertType<T>(this string value)
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

                case Type intType when intType == typeof(byte[]):
                    return (T)(object)value.Split(",").Select(x => byte.Parse(x)).ToArray();

                default:
                    throw new OPCUnsupportedType(value.GetType().Name);
            }
        }
    }
}
