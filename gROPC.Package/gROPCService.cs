using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using gROPC.Package.Exceptions;

namespace gROPC
{
    public class gROPCService
    {
        private gROPC.gRPC.OPCUAServices.OPCUAServicesClient _client;

        private string _serverURL;

        private Channel _channel;

        public int LastID
        {
            get; private set;
        }

        public gROPCService(string serverURL, int serverPort)
        {
            _serverURL = serverURL + ":" + serverPort;

            _channel = new Channel(_serverURL, ChannelCredentials.Insecure);

            _client = new gRPC.OPCUAServices.OPCUAServicesClient(_channel);
        }

        ~gROPCService()
        {
            _channel.ShutdownAsync().Wait();
        }

        public gROPCService(string serverURL)
        {
            _serverURL = serverURL;

            Channel channel = new Channel(_serverURL, ChannelCredentials.Insecure);

            _client = new gRPC.OPCUAServices.OPCUAServicesClient(channel);
        }

        public T Read<T>(string value) where T : IConvertible
        {
            try
            {
                return _convertType<T>(_client.ReadValue(new gRPC.ReadValueRequest
                {
                    NodeValue = value
                }).Response);
            }catch(Exception ex)
            {
                throw new gRPCDisconnected("Cannot read, communication cannot be establiched");
            }
        }

        private static T _convertType<T>(string value) where T : IConvertible
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

        public gROPC.Package.gROPCSubscription<T> Subscribe<T>(string nodeValue) where T : IConvertible
        {
            return new Package.gROPCSubscription<T>(this._client, nodeValue);
        }

        public void Write<T>(string nodeValue, T value)
        {
            switch (value)
            {
                case int i:
                    try
                    {
                        _writeInt(nodeValue, i);
                    }catch(Exception ex)
                    {
                        throw new gRPCDisconnected("Cannot write, communication cannot be establiched");
                    }
                    return;
                case double d:
                    try
                    {
                        _writeDouble(nodeValue, d);
                    }catch (Exception ex)
                    {
                        throw new gRPCDisconnected("Cannot write, communication cannot be establiched");
                    }
                    return;
                case float d:
                    try
                    {
                        _writeDouble(nodeValue, d);
                    }catch(Exception ex)
                    {
                        throw new gRPCDisconnected("Cannot write, communication cannot be establiched");
                    }
                    return;
                case bool b:
                    try
                    {
                        _writeBool(nodeValue, b);
                    }catch (Exception ex)
                    {
                        throw new gRPCDisconnected("Cannot write, communication cannot be establiched");
                    }
                    return;
                case string s:
                    try
                    {
                        _writeString(nodeValue, s);
                    }catch (Exception ex)
                    {
                        throw new gRPCDisconnected("Cannot write, communication cannot be establiched");
                    }
                    return;
                default:
                    throw new OPCUnsupportedType(value.GetType().Name);
            }
        }

        private void _writeString(string nodeValue, string _value)
        {
            var response = this._client.WriteValue(new gRPC.WriteValueRequest
            {
                NodeValue = nodeValue,
                Value = _value,
                Type = "string"
            }).Response;

            switch (response)
            {
                case "OK":
                    break;
                case "UNAUTHORIZED":
                    throw new OPCUnauthorizedOperation(response);
                    break;
                case "WRONG_TYPE":
                    throw new OPCWrongType(response);
                    break;
                case "UNKNOWN_TYPE":
                    throw new OPCUnknownType(response);
                    break;
                default:
                    throw new Exception("Unknown exception: something went wrong");
                    break;
            }
        }

        private void _writeInt(string nodeValue, int _value)
        {
            var response = this._client.WriteValue(new gRPC.WriteValueRequest
            {
                NodeValue = nodeValue,
                Value = _value.ToString(),
                Type = "int"
            }).Response;

            switch (response)
            {
                case "OK":
                    break;
                case "UNAUTHORIZED":
                    throw new OPCUnauthorizedOperation(response);
                    break;
                case "WRONG_TYPE":
                    throw new OPCWrongType(response);
                    break;
                case "UNKNOWN_TYPE":
                    throw new OPCUnknownType(response);
                    break;
                default:
                    throw new Exception("Unknown exception: something went wrong");
                    break;
            }
        }

        private void _writeDouble(string nodeValue, double _value)
        {
            var response = this._client.WriteValue(new gRPC.WriteValueRequest
            {
                NodeValue = nodeValue,
                Value = _value.ToString(),
                Type = "double"
            }).Response;

            switch (response)
            {
                case "OK":
                    break;
                case "UNAUTHORIZED":
                    throw new OPCUnauthorizedOperation(response);
                    break;
                case "WRONG_TYPE":
                    throw new OPCWrongType(response);
                    break;
                case "UNKNOWN_TYPE":
                    throw new OPCUnknownType(response);
                    break;
                default:
                    throw new Exception("Unknown exception: something went wrong");
                    break;
            }
        }

        private void _writeBool(string nodeValue, bool _value)
        {
            var response = this._client.WriteValue(new gRPC.WriteValueRequest
            {
                NodeValue = nodeValue,
                Value = _value.ToString(),
                Type = "bool"
            }).Response;

            switch (response)
            {
                case "OK":
                    break;
                case "UNAUTHORIZED":
                    throw new OPCUnauthorizedOperation(response);
                    break;
                case "WRONG_TYPE":
                    throw new OPCWrongType(response);
                    break;
                case "UNKNOWN_TYPE":
                    throw new OPCUnknownType(response);
                    break;
                default:
                    throw new Exception("Unknown exception: something went wrong");
                    break;
            }
        }
    }
}