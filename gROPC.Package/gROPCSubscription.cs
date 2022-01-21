using gROPC.Package;
using gROPC.Package.Exceptions;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gROPC
{
    /// <summary>
    /// Subscribe to an OPC value
    /// </summary>
    /// <typeparam name="T">Type of the variable observed</typeparam>
    public class gROPCSubscription<T>
    {
        private string _GUID;
        /// <summary>
        /// GUID of the connection between the package and the gROPC server
        /// </summary>
        public string GUID {
            get { return _GUID.ToString(); }
            set { }
        }

        private string _nodeName;
        /// <summary>
        /// Node name observed by the class
        /// </summary>
        public string NodeValue {
            get { return _nodeName; }
            set { }
        }

        private string _returnedValues = "";
        private string _returnedValuesSeparator = "<#.#>";
        public List<string> ReturnedValues
        {
            get { return new List<string>(_returnedValues.Split(_returnedValuesSeparator)); }
            set {
                if (_subscribed)
                    throw new Exception("The subscription is already running");

                foreach(var i in value)
                {
                    if (i.Contains(_returnedValuesSeparator))
                        throw new ArgumentException("The separator is contained into this array : \"" + _returnedValuesSeparator + "\"");
                }

                _returnedValues = string.Join(_returnedValuesSeparator, value);
            }
        }

        private bool _subscribed;
        /// <summary>
        /// Gives us the state of the communication, is subscribed or not
        /// </summary>
        public  bool Subscribed {
            get { return this._subscribed; }
            set { }
        }

        private bool _muted;
        /// <summary>
        /// Muting the event turns off the event throwing for onChangeValue
        /// </summary>
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

        /// <summary>
        /// Background task for the onChangeValue event
        /// </summary>
        private Task _subscriptionThread;

        /// <summary>
        /// gRPC client to connect to gROPC server
        /// </summary>
        private gRPC.OPCUAServices.OPCUAServicesClient _client;

        /// <summary>
        /// Timeout before retying to reconnect
        /// </summary>
        private int _reconnectionTimeout;

        /// <summary>
        /// Number of reconnection allowed before giving up
        /// </summary>
        private int _reconnectionMaxAttempts;

        /// <summary>
        /// Number of attempts of reconnection since the last connection
        /// </summary>
        private int _reconnectionAttempt = 0;

        /// <summary>
        /// Event throwed when the OPC's value change
        /// </summary>
        public event EventHandler<SubscriptionResponse<T>> onChangeValue;

        /// <summary>
        /// Event throwed when the subscription has normally disconnected
        /// </summary>
        public event EventHandler onDisconnect;

        /// <summary>
        /// Event throwed when the subscription has normally connected
        /// The GUID may change during the execution
        /// </summary>
        public event EventHandler onConnect;

        /// <summary>
        /// Event throwed when the subscription has a loss of connection
        /// </summary>
        public event EventHandler<int> onConnectionLost;


        public event EventHandler<Exception> onError;

        /// <summary>
        /// Create an object ready to subscribe to a value
        /// </summary>
        /// <param name="client">gRPC object for connecting to the server</param>
        /// <param name="nodeName">Node name to observe</param>
        public gROPCSubscription(gRPC.OPCUAServices.OPCUAServicesClient client, string nodeName){
            _client = client;
            _nodeName = nodeName;

            _reconnectionTimeout = 2200;
            _reconnectionMaxAttempts = -1;


        }

        public gROPCSubscription(gRPC.OPCUAServices.OPCUAServicesClient client, string nodeName, int reconnectionTimeout, int reconnectionMaxAttempts)
        {
            _client = client;
            _nodeName = nodeName;

            _reconnectionTimeout = reconnectionTimeout;
            _reconnectionMaxAttempts = reconnectionMaxAttempts;
        }

        /// <summary>
        /// Subscribe to a value on the gROPC server
        /// </summary>
        /// <returns>Subscription object</returns>
        public gROPCSubscription<T> Subscribe()
        {
            _muted = false;
            _subscribed = true;

            _subscriptionThread = _subscriptionThreadAsync();

            return this;
        }

        /// <summary>
        /// Thread for catching onChangeValue event
        /// </summary>
        /// <returns></returns>
        private async Task _subscriptionThreadAsync()
        {
            if (!_subscribed)
                return;
            try
            {
                try
                {
                    using (var result = _client.SubscribeValue(new gRPC.SubscribeValueRequest
                    {
                        NodeValue = _nodeName,
                        ReturnedValues = _returnedValues
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

                        if (this._GUID == "-1")
                        {
                            throw new OPCUnknownNode(result.ResponseStream.Current.Response);
                        }

                        // onConnected throwed
                        _subscribed = true;
                        onConnect?.Invoke(this, null);

                        bool streamActive = true;

                        // Getting every values form the gRPC stream
                        while (streamActive && _subscribed)
                        {
                            try
                            {
                                // try to get the latest value
                                streamActive = await result.ResponseStream.MoveNext();
                            }
                            catch (Exception ex)
                            {
                                throw new gRPCDisconnected("An error was encountered while streaming data");
                            }

                            if (streamActive && _subscribed)
                            {
                                // getting the value returned by the gROPC server
                                gRPC.SubscribeValueResponse feature = result.ResponseStream.Current;

                                // if it's not muted or unsubscribed
                                if (!_muted && _subscribed)
                                {
                                    string[] values = feature.Response.Split(_returnedValuesSeparator);
                                    _onRecieveValue(values[0], values.SubArray(1));
                                }
                            }
                        }
                    }
                }
                catch (gRPCDisconnected ex)
                {
                    // if we had a connection issue
                    _reconnectionAttempt++;

                    if (_reconnectionMaxAttempts == -1 || _reconnectionAttempt < _reconnectionMaxAttempts)
                    {
                        // if we can try to reconnect instead of throwing an error
                        onConnectionLost?.Invoke(this, _reconnectionAttempt);

                        System.Threading.Thread.Sleep(_reconnectionTimeout);

                        // recursively redo the connection protocol
                        _subscriptionThread = _subscriptionThreadAsync();
                    }
                    else
                    {
                        throw new gRPCDisconnected(ex.Reason);
                    }
                }
            }catch(Exception ex)
            {
                _subscribed = false;

                // throw the error on client side
                if (onError == null)
                    throw ex;
                else
                    onError.Invoke(this, ex);
            }
        }

        /// <summary>
        /// Convert the type oh the value recieve from the gROPC server and throw the event onChangeValue
        /// </summary>
        /// <param name="value">value that has changed</param>
        private void _onRecieveValue(string value, string[] valuesReturned)
        {
            var response = new SubscriptionResponse<T>()
            {
                responseValue = value.ConvertType<T>(),
                responsesAssociated = new Dictionary<string, string>()
            };

            for(var index = 0; index < valuesReturned.Length; index++)
            {
                response.responsesAssociated.Add(ReturnedValues[index],valuesReturned[index]);
            }

            onChangeValue?.Invoke(this, response);
        }

        /// <summary>
        /// Unsubscribe the current subscription
        /// </summary>
        /// <exception cref=""></exception>
        /// <seealso cref="Subscribe"/>
        public void Unsubscribe()
        {
            if (!_subscribed)
                return;
            try
            {
                _client.UnsubscribeValue(new gRPC.UnsibscribeValueRequest
                {
                    SubscriptionId = this._GUID
                });
            }catch(Exception ex)
                { }

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
