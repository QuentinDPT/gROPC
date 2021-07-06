using gROPC.Package.Exceptions;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace gROPC
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

        private int _reconnectionTimeout;

        private int _reconnectionMaxAttempts;

        private int _reconnectionAttempt = 0;


        public event EventHandler<T> onChangeValue;

        public event EventHandler onDisconnect;

        public event EventHandler onConnect;

        public event EventHandler<int> onConnectionLost;


        public gROPCSubscription(gROPC.gRPC.OPCUAServices.OPCUAServicesClient client, string nodeValue){
            _client = client;
            _nodeValue = nodeValue;

            _reconnectionTimeout = 2200;
            _reconnectionMaxAttempts = -1;
        }

        public gROPCSubscription(gRPC.OPCUAServices.OPCUAServicesClient client, string nodeValue, int reconnectionTimeout, int reconnectionMaxAttempts)
        {
            _client = client;
            _nodeValue = nodeValue;

            _reconnectionTimeout = reconnectionTimeout;
            _reconnectionMaxAttempts = reconnectionMaxAttempts;
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
            try
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
                        _reconnectionAttempt = 0;
                    }
                    catch (Exception ex)
                    {
                        throw new gRPCDisconnected("Cannot connect to the server endpoint");
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
                        }
                        catch (Exception ex)
                        {
                            throw new gRPCDisconnected("An error was encountered while streaming data");
                        }

                        gRPC.SubscribeValueResponse feature = result.ResponseStream.Current;

                        if (!_muted && _subscribed)
                            _onRecieveValue(feature.Response);
                    }
                }
            }catch(gRPCDisconnected ex){
                _reconnectionAttempt++;

                if (_reconnectionMaxAttempts == -1 || _reconnectionAttempt < _reconnectionMaxAttempts)
                {
                    onConnectionLost?.Invoke(this, _reconnectionAttempt);
                    System.Threading.Thread.Sleep(_reconnectionTimeout);
                    _subscriptionThread = _subscriptionThreadAsync();
                }
                else
                {
                    throw new gRPCDisconnected(ex.Reason) ;
                }
            }
        }

        private void _onRecieveValue(string value)
        {
            T convertedValue = Package.gROPCConverter.ConvertType<T>(value);

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
