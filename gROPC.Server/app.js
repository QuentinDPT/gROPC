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
const config = require("config");
var OPCUA = require("./OPCUA");
var grpc = require("@grpc/grpc-js");
var protoLoader = require('@grpc/proto-loader');
var PROTO_PATH = __dirname + "/protos/opcua.proto";
var serverURL = config.get("server.ip") + ":" + config.get("server.port");
var OPCURL = config.get("OPC.url");
var writeWhiteList = config.get("whitelist");
for (let n of writeWhiteList) {
    OPCUA.OPCUA_whitelist.push(n.toUpperCase());
}
var packageDefinition = protoLoader.loadSync(PROTO_PATH, {
    keepCase: true,
    longs: String,
    enums: String,
    defaults: true,
    oneofs: true
});
var opcuaProto = grpc.loadPackageDefinition(packageDefinition).OPCUA;
var OPC = new OPCUA.OPCUA(OPCURL);
function _readValue(call, callback) {
    return __awaiter(this, void 0, void 0, function* () {
        let result = yield OPC.readValue(call.request.nodeValue);
        callback(null, { response: result });
    });
}
var observerID = 1;
var valuesTracked = [];
function _subscribeValue(call, callback) {
    return __awaiter(this, void 0, void 0, function* () {
        let subid = observerID++;
        let _opcenv = yield OPC.subscribeValue(call.request.nodeValue, function (result) {
            call.write({
                subsciptionId: subid,
                response: result
            });
        });
        console.log("new subscription " + subid + " (gRPC)");
        call.write({
            subsciptionId: subid,
            response: ""
        });
        valuesTracked.push({
            nodeValue: call.request.nodeValue,
            subscriptionID: subid,
            thread: call,
            opcenv: _opcenv
        });
    });
}
function _unsubscribeValue(call, callback) {
    return __awaiter(this, void 0, void 0, function* () {
        console.log("unsubscribe " + call.request.subscriptionId + " (gRPC)");
        let th = valuesTracked.find(x => x.subscriptionID == call.request.subscriptionId);
        if (th == null)
            return;
        yield OPC.unsubscribeValue(th.opcenv.subscriptionId);
        th.thread.end();
        callback(null, null);
    });
}
function _writeValue(call, callback) {
    return __awaiter(this, void 0, void 0, function* () {
        let result = OPC.writeValue(call.request.nodeValue, call.request.value, call.request.type);
        callback(null, { response: result });
    });
}
/**
 * Starts an RPC server that receives requests for the Greeter service at the
 * sample server port
 */
function main() {
    return __awaiter(this, void 0, void 0, function* () {
        // Connect to the OPC server
        yield OPC.connect();
        // Starting the gRPC server
        let server = new grpc.Server();
        server.addService(opcuaProto.OPCUAServices.service, {
            readValue: _readValue,
            subscribeValue: _subscribeValue,
            unsubscribeValue: _unsubscribeValue,
            writeValue: _writeValue
        });
        server.bindAsync(serverURL, grpc.ServerCredentials.createInsecure(), () => {
            server.start();
        });
    });
}
main();
//*/
//# sourceMappingURL=app.js.map