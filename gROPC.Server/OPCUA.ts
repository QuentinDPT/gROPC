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

            public connected: boolean;


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

                subscription.on("started", function () {
                    console.log("subscription started - subscriptionId=", subscription.subscriptionId);
                }).on("keepalive", function () {
                    console.log("keepalive");
                }).on("terminated", function () {
                    console.log("terminated");
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
                var observation = this._subscriptions.find(x => x.subscriptionId == subscriptionId);
                if (observation == null) {
                    return;
                }
                await observation.terminate();
                this._subscriptions = this._subscriptions.filter(function (val, idx, arr) {
                    return val.subscriptionId != subscriptionId;
                });
            }

            public writeValue(nodeName: string, value: string, type: string) {

                let upperNodeName = nodeName.toUpperCase();
                if (___OPCUA_WHITELIST.findIndex(x => x == upperNodeName) == -1)
                    return "NOK";

                let readInProgressData: Variant = null;

                switch (type) {
                    case "string":
                        readInProgressData = {
                            dataType: DataType.String,
                            value: value
                        } as Variant;
                        break;
                    case "double":
                        readInProgressData = {
                            dataType: DataType.Double,
                            value: value
                        } as Variant;
                        break;
                    case "bool":
                        readInProgressData = {
                            dataType: DataType.Boolean,
                            value: value
                        } as Variant;
                        break;
                    case "int":
                        readInProgressData = {
                            dataType: DataType.Int16,
                            value: parseInt(value)
                        } as Variant;
                        break;
                    default:
                        return "NOK";
                }

                this._session.writeSingleNode(nodeName, readInProgressData);

                return "OK";
            }
        },
    OPCUA_whitelist: ___OPCUA_WHITELIST
}