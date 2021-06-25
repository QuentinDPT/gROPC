"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
const node_opcua_1 = require("node-opcua");
let ___OPCUA_WHITELIST = [];
module.exports = {
    OPCUA: class OPCUA {
        constructor(endpoint) {
            const options = {
                applicationName: "OPCServer",
                connectionStrategy: {
                    initialDelay: 1000,
                    maxRetry: 1
                },
                securityMode: node_opcua_1.MessageSecurityMode.None,
                securityPolicy: node_opcua_1.SecurityPolicy.None,
                endpointMustExist: false,
                keepSessionAlive: true
            };
            this.connected = false;
            this._endpoint = endpoint;
            this._client = node_opcua_1.OPCUAClient.create(options);
            this._session = null;
            this._subscriptions = [];
        }
        connect() {
            return __awaiter(this, void 0, void 0, function* () {
                yield this._client.connect(this._endpoint);
                this._session = yield this._client.createSession();
            });
        }
        readValue(nodeName) {
            return __awaiter(this, void 0, void 0, function* () {
                const dataValue = yield this._session.readVariableValue(nodeName);
                return dataValue.value.value.toString();
            });
        }
        subscribeValue(nodeName, callback) {
            return __awaiter(this, void 0, void 0, function* () {
                const subscription = node_opcua_1.ClientSubscription.create(this._session, {
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
                const itemToMonitor = {
                    nodeId: nodeName,
                    attributeId: node_opcua_1.AttributeIds.Value
                };
                const parameters = {
                    samplingInterval: 100,
                    discardOldest: true,
                    queueSize: 10
                };
                const monitoredItem = node_opcua_1.ClientMonitoredItem.create(subscription, itemToMonitor, parameters, node_opcua_1.TimestampsToReturn.Both);
                monitoredItem.on("changed", (dataValue) => {
                    callback(dataValue.value.value.toString());
                });
                this._subscriptions.push(subscription);
                return subscription;
            });
        }
        unsubscribeValue(subscriptionId) {
            return __awaiter(this, void 0, void 0, function* () {
                var observation = this._subscriptions.find(x => x.subscriptionId == subscriptionId);
                if (observation == null) {
                    return;
                }
                yield observation.terminate();
                this._subscriptions = this._subscriptions.filter(function (val, idx, arr) {
                    return val.subscriptionId != subscriptionId;
                });
            });
        }
        writeValue(nodeName, value, type) {
            let upperNodeName = nodeName.toUpperCase();
            if (___OPCUA_WHITELIST.findIndex(x => x == upperNodeName) == -1)
                return "NOK";
            let readInProgressData = null;
            switch (type) {
                case "string":
                    readInProgressData = {
                        dataType: node_opcua_1.DataType.String,
                        value: value
                    };
                    break;
                case "double":
                    readInProgressData = {
                        dataType: node_opcua_1.DataType.Double,
                        value: value
                    };
                    break;
                case "bool":
                    readInProgressData = {
                        dataType: node_opcua_1.DataType.Boolean,
                        value: value
                    };
                    break;
                case "int":
                    readInProgressData = {
                        dataType: node_opcua_1.DataType.Int16,
                        value: parseInt(value)
                    };
                    break;
                default:
                    return "NOK";
            }
            this._session.writeSingleNode(nodeName, readInProgressData);
            return "OK";
        }
    },
    OPCUA_whitelist: ___OPCUA_WHITELIST
};
//# sourceMappingURL=OPCUA.js.map