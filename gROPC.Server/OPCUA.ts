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

let ___OPCUA_WHITELIST: string[] = [];

module.exports = {
    OPCUA:
        class OPCUA {
            private _endpoint: string;

            private _client;

            private _session;

            private _subscriptions: ClientSubscription[];

            private _nodeValuesTypes = [];

            public connected: boolean;

            public _logger;
            



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

                this._logger = console;
            }

            public async connect() {
                await this._client.connect(this._endpoint);
                this._session = await this._client.createSession();
            }

            public async readValue(nodeName: string) {
                const dataValue = await this._session.readVariableValue(nodeName);
                return dataValue.value.value.toString();
            }


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

                subscription.on("started", function () {
                    __self._logger.info("new subscription " + subscription.subscriptionId + " (OPCUA)");
                    subID = subscription.subscriptionId;
                }).on("keepalive", function () {
                    __self._logger.info("keepalive " + subscription.subscriptionId + " (OPCUA)");
                }).on("terminated", function () {
                    __self._logger.info("terminated " + subID + " (OPCUA)");
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

                const monitoredItem = ClientMonitoredItem.create(
                    subscription,
                    itemToMonitor,
                    parameters,
                    TimestampsToReturn.Both
                );

                monitoredItem.on("changed", (dataValue: DataValue) => {
                    callback(dataValue.value.value.toString());
                });

                this._subscriptions.push(subscription);

                return subscription;
            }

            public async unsubscribeValue(subscriptionId: number) {
                let observation = this._subscriptions.find(x => x.subscriptionId == subscriptionId);
                if (observation == null) {
                    return;
                }
                await observation.terminate();
                this._subscriptions = this._subscriptions.filter(function (val, idx, arr) {
                    return val.subscriptionId != subscriptionId;
                });
            }

            public async writeValue(nodeName: string, value: string, type: string) {

                let upperNodeName = nodeName.toUpperCase();
                if (___OPCUA_WHITELIST.findIndex(x => x == upperNodeName) == -1) {
                    this._logger.error("non whitelisted node value : " + value + " (OPCUA > Write)");
                    return "UNAUTHORIZED";
                }

                let readInProgressData: Variant = null;

                let typesCache = this._nodeValuesTypes.find(x => x.node == nodeName);

                if (typesCache == null) {
                    const dataValue = await this._session.readVariableValue(nodeName);

                    typesCache = { "node": nodeName, "type": dataValue.value.dataType};
                    this._nodeValuesTypes.push(typesCache);
                }

                switch (type) {
                    case "string":
                        if (typesCache.type != DataType.String) {
                            this._logger.error("wrong type : " + value + " (OPCUA > Write)");
                            return "WRONG_TYPE";
                        }
                        readInProgressData = {
                            dataType: typesCache.type,
                            value: value
                        } as Variant;
                        break;
                    case "double":
                        if (typesCache.type != DataType.Double && typesCache.type != DataType.Float) {
                            this._logger.error("wrong type : " + value + " (OPCUA > Write)");
                            return "WRONG_TYPE";
                        }
                        readInProgressData = {
                            dataType: typesCache.type,
                            value: value
                        } as Variant;
                        break;
                    case "bool":
                        if (typesCache.type != DataType.Boolean) {
                            this._logger.error("wrong type : " + value + " (OPCUA > Write)");
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
                            this._logger.error("wrong type : " + value + " (OPCUA > Write)");
                            return "WRONG_TYPE";
                        }
                        readInProgressData = {
                            dataType: typesCache.type,
                            value: parseInt(value)
                        } as Variant;
                        break;
                    default:
                        this._logger.error("unknown variable type (OPCUA > Write)");
                        return "UNKNOWN_TYPE";
                }

                this._session.writeSingleNode(nodeName, readInProgressData);

                return "OK";
            }
        },
    OPCUA_whitelist: ___OPCUA_WHITELIST
}