/// OPCUA.ts
/// OPCUA permet de contacter des automates via OPC

import {
    OPCUAClient,
    MessageSecurityMode, SecurityPolicy,
    AttributeIds,
    makeBrowsePath,
    ClientSubscription,
    TimestampsToReturn,
    MonitoringParametersOptions,
    ReadValueId,
    ReadValueIdOptions,
    ClientMonitoredItem,
    DataValue,

    Variant,
    DataType
} from "node-opcua";

// whitelisted nodes allowed to be writed
let ___OPCUA_WHITELIST: string[] = [];

module.exports = {
    OPCUA:
        class OPCUA {
            // URL serveur pour pouvoir extraire les données
            private _endpoint: string;

            // gRPC client
            private _client;

            // session opened to send information
            private _session;

            // list of subscription associated to the server
            private _subscriptions: ClientSubscription[];

            // cache that contain the node name and the node type
            private _nodeValuesTypes = [];

            public connected: boolean;

            // logger object for logging stuff
            public logger;
            

            /**
             * create an object to contact the OPC server
             * 
             * @param endpoint OPC's address
             */
            public constructor(endpoint: string) {
                const options = {
                    applicationName: "OPCServer",
                    connectionStrategy: {
                        initialDelay: 1000,
                        maxRetry: 1
                    },
                    securityMode: MessageSecurityMode.None,
                    securityPolicy: SecurityPolicy.None,
                    endpointMustExist: false,
                    keepSessionAlive: true
                };

                this.connected = false;
                this._endpoint = endpoint;
                this._client = OPCUAClient.create(options);
                this._session = null;
                this._subscriptions = [];

                this.logger = console;
            }

            /**
             * connects with the OPC server
             */
            public async connect() {
                await this._client.connect(this._endpoint);
                this._session = await this._client.createSession();
            }

            /**
             * Read a value from the OPC server
             * 
             * @param nodeName name of the node to read
             */
            public async readValue(nodeName: string) {
                const dataValue = await this._session.readVariableValue(nodeName);
                return dataValue.value.value.toString();
            }

            /**
             * Subscribe to a value on the OPC
             * 
             * @param nodeName name of the node to listen
             * @param callback function to call when the observer detect that the value has changed
             */
            public async subscribeValue(nodeName: string, callback) {

                const subscription = ClientSubscription.create(this._session, {
                    requestedPublishingInterval: 1000,
                    requestedLifetimeCount: 100,
                    requestedMaxKeepAliveCount: 10,
                    maxNotificationsPerPublish: 100,
                    publishingEnabled: true,
                    priority: 10
                });

                let subID = 0;

                const __self = this;

                // starting the server
                // implementing reaction to differents events throwed by the OPC
                subscription.on("started", function () {
                    __self.logger.info("new subscription " + subscription.subscriptionId + " (OPCUA)");
                    subID = subscription.subscriptionId;
                }).on("keepalive", function () {
                    __self.logger.info("keepalive " + subscription.subscriptionId + " (OPCUA)");
                }).on("terminated", function () {
                    __self.logger.info("terminated " + subID + " (OPCUA)");
                });


                const itemToMonitor: ReadValueId | ReadValueIdOptions = {
                    nodeId: nodeName,
                    attributeId: AttributeIds.Value
                };
                const parameters: MonitoringParametersOptions = {
                    samplingInterval: 100,
                    discardOldest: true,
                    queueSize: 10
                };

                // creating the value to observe
                const monitoredItem = ClientMonitoredItem.create(
                    subscription,
                    itemToMonitor,
                    parameters,
                    TimestampsToReturn.Both
                );

                monitoredItem.on("changed", (dataValue: DataValue) => {
                    callback(dataValue.value.value.toString());
                });

                // add the subscription to the server's subscription list
                this._subscriptions.push(subscription);

                return subscription;
            }

            /**
             * unsubscribe to a value
             * 
             * @param subscriptionId ID of the OPC
             */
            public async unsubscribeValue(subscriptionId: number) {
                // lf subscription
                let observation = this._subscriptions.find(x => x.subscriptionId == subscriptionId);
                if (observation == null) {
                    return;
                }

                // destroys it
                await observation.terminate();

                // remove it from the subscribed values
                this._subscriptions = this._subscriptions.filter(function (val, idx, arr) {
                    return val.subscriptionId != subscriptionId;
                });
            }

            /**
             * Write a value on the OPC (it has to be whitelisted)
             * 
             * @param nodeName name of the node to write the value
             * @param value value to write
             * @param type type of the value to write (int, string, double, bool)
             */
            public async writeValue(nodeName: string, value: string, type: string) {

                // see if the value is whitelisted, then allow the writing
                let upperNodeName = nodeName.toUpperCase();
                if (___OPCUA_WHITELIST.findIndex(x => x == upperNodeName) == -1) {
                    this.logger.error("non whitelisted node value : " + value + " (OPCUA > Write)");
                    return "UNAUTHORIZED";
                }

                let readInProgressData: Variant = null;

                // see if the nodeName exists into the node cache
                let typesCache = this._nodeValuesTypes.find(x => x.node == nodeName);

                if (typesCache == null) {
                    // getting the type of the node name
                    const dataValue = await this._session.readVariableValue(nodeName);

                    typesCache = { "node": nodeName, "type": dataValue.value.dataType};
                    this._nodeValuesTypes.push(typesCache);
                }

                // switch on type of the request and compare it to the actual type of the node name
                switch (type) {
                    case "string":
                        if (typesCache.type != DataType.String) {
                            this.logger.error("wrong type : " + value + " (OPCUA > Write)");
                            return "WRONG_TYPE";
                        }
                        readInProgressData = {
                            dataType: typesCache.type,
                            value: value
                        } as Variant;
                        break;
                    case "double":
                        if (typesCache.type != DataType.Double && typesCache.type != DataType.Float) {
                            this.logger.error("wrong type : " + value + " (OPCUA > Write)");
                            return "WRONG_TYPE";
                        }
                        readInProgressData = {
                            dataType: typesCache.type,
                            value: value
                        } as Variant;
                        break;
                    case "bool":
                        if (typesCache.type != DataType.Boolean) {
                            this.logger.error("wrong type : " + value + " (OPCUA > Write)");
                            return "WRONG_TYPE";
                        }
                        readInProgressData = {
                            dataType: typesCache.type,
                            value: value
                        } as Variant;
                        break;
                    case "int":
                        if (
                            typesCache.type != DataType.Int16  && typesCache.type != DataType.Int32  && typesCache.type != DataType.Int64  &&
                            typesCache.type != DataType.UInt16 && typesCache.type != DataType.UInt32 && typesCache.type != DataType.UInt64
                           )
                        {
                            this.logger.error("wrong type : " + value + " (OPCUA > Write)");
                            return "WRONG_TYPE";
                        }
                        readInProgressData = {
                            dataType: typesCache.type,
                            value: parseInt(value)
                        } as Variant;
                        break;
                    default:
                        // the type asked dosent exists on the gROPC implementation
                        this.logger.error("unknown variable type (OPCUA > Write)");
                        return "UNKNOWN_TYPE";
                }

                // writing the value on the OPC
                this._session.writeSingleNode(nodeName, readInProgressData);

                // returning "OK" is the only way for the client to not to crash
                return "OK";
            }
        },
    OPCUA_whitelist: ___OPCUA_WHITELIST
}