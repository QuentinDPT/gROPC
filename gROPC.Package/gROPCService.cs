using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;

namespace gROPC
{
    public class gROPCService
    {
        private List<int> _subscriptions;

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
                case bool b:
                    _writeBool(nodeValue, b);
                    return;
                case string s:
                    _writeString(nodeValue, s);
                    return;
                default:
                    throw new Exception("unsupported type");
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

            if(response != "OK")
            {
                throw new Exception(response);
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

            if (response != "OK")
            {
                throw new Exception(response);
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

            if (response != "OK")
            {
                throw new Exception(response);
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

            if (response != "OK")
            {
                throw new Exception(response);
            }
        }
    }
}