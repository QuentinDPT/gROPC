using gROPC.Package.Exceptions;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace gROPC.Package
{
    public class gROPCSubscription<T> where T : IConvertible
    {
        private string _GUID;
        public string GUID {
            get { return _GUID.ToString(); }
            set { }
        }

        private string _nodeValue;
        public string NodeValue {
            get { return _nodeValue; }
            set { }
        }

        private bool _subscribed;
        public  bool Subscribed {
            get { return this._subscribed; }
            set { }
        }

        private bool _muted;
        public bool EventMuted {
            get { return _muted; }
            set {
                if (value && !_muted)
                {
                    MuteChangeValueEvent();
                }
                else if(!value && _muted)
                {
                    UnmuteChangeValueEvent();
                }
            }
        }

        private System.Threading.Tasks.Task _subscriptionThread;

        private gROPC.gRPC.OPCUAServices.OPCUAServicesClient _client;

        public event EventHandler<T> onChangeValue;

        public event EventHandler onDisconnect;

        public event EventHandler onConnect;

        public gROPCSubscription(gROPC.gRPC.OPCUAServices.OPCUAServicesClient client, string nodeValue){
            _client = client;
            _nodeValue = nodeValue;
        }

        public gROPCSubscription<T> Subscribe()
        {
            _muted = false;
            _subscribed = true;

            _subscriptionThread = _subscriptionThreadAsync();

            return this;
        }

        private async System.Threading.Tasks.Task _subscriptionThreadAsync()
        {
            using (var result = _client.SubscribeValue(new gRPC.SubscribeValueRequest
            {
                NodeValue = _nodeValue
            })
                )
            {
                try
                {
                    await result.ResponseStream.MoveNext();
                }
                catch (Exception ex)
                {
                    throw new gROPC.Package.Exceptions.gRPCDisconnected("Cannot connect to the server endpoint");
                }

                this._GUID = result.ResponseStream.Current.SubsciptionId;

                // onConnected throwed
                _subscribed = true;
                onConnect?.Invoke(this, null);

                bool streamActive = true;

                while (streamActive)
                {
                    try
                    {
                        streamActive = await result.ResponseStream.MoveNext();
                    }catch(Exception ex)
                    {
                        throw new gRPCDisconnected("An error was encountered while streaming data");
                    }

                    gRPC.SubscribeValueResponse feature = result.ResponseStream.Current;

                    if(!_muted && _subscribed)
                        _onRecieveValue(feature.Response);
                }
            }
        }

        private void _onRecieveValue(string value)
        {
            T convertedValue = gROPCConverter.ConvertType<T>(value);

            onChangeValue?.Invoke(this, convertedValue);
        }


        public void Unsubscribe()
        {
            if (!_subscribed)
                return;

            _client.UnsubscribeValue(new gRPC.UnsibscribeValueRequest
            {
                SubscriptionId = this._GUID
            });

            _subscribed = false;

            onDisconnect?.Invoke(this, null);
        }

        public void MuteChangeValueEvent()
        {
            // Already muted
            if (_muted)
                return;

            _muted = true;
        }

        public void UnmuteChangeValueEvent()
        {
            // Already unmuted
            if (!_muted)
                return;

            _muted = false;
        }
    }
}
