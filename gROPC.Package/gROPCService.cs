using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using gROPC.Package.Exceptions;

namespace gROPC
{
    /// <summary>
    /// gROPC service to connect to the gROPC server
    /// </summary>
    public class gROPCService
    {
        /// <summary>
        /// gROPC client
        /// </summary>
        private gRPC.OPCUAServices.OPCUAServicesClient _client;

        /// <summary>
        /// URL of the gROPC server
        /// </summary>
        private string _serverURL;

        /// <summary>
        /// Represents the connection between the package and the gROPC server
        /// </summary>
        private Channel _channel;

        /// <summary>
        /// Timeout before retying to reconnect
        /// </summary>
        private int _reconnectionTimeout;

        /// <summary>
        /// Number of reconnection allowed before giving up
        /// </summary>
        private int _reconnectionMaxAttempts;

        /// <summary>
        /// Connects to a gROPC server
        /// </summary>
        /// <param name="serverURL">URL of the gROPC</param>
        public gROPCService(string serverURL)
        {
            _serverURL = serverURL;

            _channel = new Channel(_serverURL, ChannelCredentials.Insecure);

            _client = new gRPC.OPCUAServices.OPCUAServicesClient(_channel);

            _reconnectionMaxAttempts = -1;

            _reconnectionTimeout = 1600;
        }

        /// <summary>
        /// Connects to a gROPC server
        /// </summary>
        /// <param name="serverURL">URL of the server</param>
        /// <param name="serverPort">Port used for the server</param>
        public gROPCService(string serverURL, int serverPort) : this(serverURL + ":" + serverPort)
            { }

        /// <summary>
        /// Connects to a gROPC server, with reconnection parameters
        /// </summary>
        /// <param name="serverURL">URL of the server</param>
        /// <param name="serverPort">Port used for the server</param>
        /// <param name="reconnectionTimeout">Timeout before retying to connect</param>
        /// <param name="reconnectionMaxAttempts">Number of reconnection allowed before giving up</param>
        public gROPCService(string serverURL, int serverPort, int reconnectionTimeout, int reconnectionMaxAttempts) : this(serverURL, serverPort)
        {
            if (reconnectionTimeout <= 0)
                throw new ArgumentException("reconnectionTimeout should be greater than 0");

            if (reconnectionMaxAttempts < -1)
                reconnectionMaxAttempts = -1;

            _reconnectionTimeout = reconnectionTimeout;
            _reconnectionMaxAttempts = reconnectionMaxAttempts;
        }

        ~gROPCService()
        {
            _channel.ShutdownAsync().Wait();
        }

        /// <summary>
        /// Read a value from the OPC
        /// </summary>
        /// <typeparam name="T">Type of the variable currently readed</typeparam>
        /// <param name="nodeValue">Name of the node readed</param>
        /// <returns>value wanted</returns>
        public T Read<T>(string nodeValue) where T : IConvertible
        {
            try
            {
                return Package.gROPCUtils.ConvertType<T>(_client.ReadValue(new gRPC.ReadValueRequest
                {
                    NodeValue = nodeValue
                }).Response);
            }
            catch (OPCUnsupportedType ex) {
                throw ex;
            }
            catch(Exception ex)
            {
                throw new gRPCDisconnected("Cannot read, communication cannot be establiched");
            }
        }

        /// <summary>
        /// Subscribe to a value on the OPC
        /// </summary>
        /// <typeparam name="T">Type of the variable observed</typeparam>
        /// <param name="nodeValue">Name of the node readed</param>
        /// <returns>Subscription class</returns>
        /// <example>
        /// <code>
        /// var subscription = OPCService.Subscribe<int>(OPCValue);
        /// subscription.onChangeValue += ma_fonction;
        /// return subscription.Subscribe();
        /// </code>
        /// </example>
        public gROPCSubscription<T> Subscribe<T>(string nodeValue) where T : IConvertible
        {
            return new gROPCSubscription<T>(this._client, nodeValue, _reconnectionTimeout, _reconnectionMaxAttempts);
        }

        /// <summary>
        /// Write a value on the OPC
        /// </summary>
        /// <typeparam name="T">Type of the variable we want to write</typeparam>
        /// <param name="nodeValue">Name of the node we want to rite</param>
        /// <param name="value">Value we want to set on the OPC</param>
        public void Write<T>(string nodeValue, T value)
        {
            try
            {
                switch (value)
                {
                    case int i:
                        _writeInt(nodeValue, i);
                        return;
                    case double d:
                        _writeDouble(nodeValue, d);
                        return;
                    case float d:
                        _writeDouble(nodeValue, d);
                        return;
                    case bool b:
                        _writeBool(nodeValue, b);
                        return;
                    case string s:
                         _writeString(nodeValue, s);
                        return;
                    default:
                        throw new OPCUnsupportedType(value.GetType().Name);
                }
            }catch(Exception ex)
            {
                throw ex;
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
                    throw new OPCUnauthorizedOperation(response + " \"" + nodeValue + "\"");
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
                    throw new OPCUnauthorizedOperation(response + " \"" + nodeValue + "\"");
                    break;
                case "WRONG_TYPE":
                    throw new OPCWrongType(response);
                    break;
                case "UNKNOWN_TYPE":
                    throw new OPCUnknownType(response);
                    break;
                default:
                    throw new gRPCDisconnected("Cannot write, communication cannot be establiched");
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
                    throw new OPCUnauthorizedOperation(response + " \"" + nodeValue + "\"");
                    break;
                case "WRONG_TYPE":
                    throw new OPCWrongType(response);
                    break;
                case "UNKNOWN_TYPE":
                    throw new OPCUnknownType(response);
                    break;
                default:
                    throw new gRPCDisconnected("Cannot write, communication cannot be establiched");
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
                    throw new OPCUnauthorizedOperation(response + " \"" + nodeValue + "\"");
                    break;
                case "WRONG_TYPE":
                    throw new OPCWrongType(response);
                    break;
                case "UNKNOWN_TYPE":
                    throw new OPCUnknownType(response);
                    break;
                default:
                    throw new gRPCDisconnected("Cannot write, communication cannot be establiched");
                    break;
            }
        }
    }
}