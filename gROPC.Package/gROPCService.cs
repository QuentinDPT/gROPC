﻿using Grpc.Core;
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

        public string Read(string value)
        {
            return _client.ReadValue(new gRPC.ReadValueRequest
            {
                NodeValue = value
            }).Response;
        }

        public int SubscribeAsync(string value, Action<string> callbackFunction)
        {
            LastID = 0;

            _subscribeAsync(value, callbackFunction);

            while (LastID == 0)
            {
                System.Threading.Thread.Sleep(2);
            };

            return LastID;
        }

        private async System.Threading.Tasks.Task<int> _subscribeAsync(string value, Action<string> callbackFunction)
        {
            int id = 0;
            using (var result = _client.SubscribeValue(new gRPC.SubscribeValueRequest
            {
                NodeValue = value
            })
                )
            {
                while (await result.ResponseStream.MoveNext())
                {
                    gRPC.SubscribeValueResponse feature = result.ResponseStream.Current;
                    if (id == feature.SubsciptionId)
                        callbackFunction(feature.Response);

                    id = feature.SubsciptionId;
                    LastID = id;
                }
            }

            return id;
        }

        public void Unsubscribe(int value)
        {
            _client.UnsubscribeValue(new gRPC.UnsibscribeValueRequest
            {
                SubscriptionId = value
            });
        }

        public void Write<T>(string nodeValue, T value)
        {
            switch (value)
            {
                case int i:
                    _writeInt(nodeValue, i) ;
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