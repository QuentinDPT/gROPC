import * as config from "config";
import _ = require("lodash");

var OPCUA = require("./OPCUA");
var grpc = require("@grpc/grpc-js");
var protoLoader = require('@grpc/proto-loader');

var PROTO_PATH = __dirname + "/protos/opcua.proto";

var serverURL = config.get("server.ip") + ":" + config.get("server.port");
var OPCURL = config.get("OPC.url");
var writeWhiteList : string[] = config.get("whitelist");
for (let n of writeWhiteList) {
    OPCUA.OPCUA_whitelist.push(n.toUpperCase());
}

var packageDefinition = protoLoader.loadSync(
    PROTO_PATH,
    {
        keepCase: true,
        longs: String,
        enums: String,
        defaults: true,
        oneofs: true
    }
);
var opcuaProto = grpc.loadPackageDefinition(packageDefinition).OPCUA;

var OPC = new OPCUA.OPCUA(OPCURL);


async function _readValue(call, callback) {
    let result = await OPC.readValue(call.request.nodeValue);

    callback(null, { response: result });
}

var observerID = 1;

var valuesTracked = [];


async function _subscribeValue(call, callback) {
    let subid = observerID++;

    let _opcenv = await OPC.subscribeValue(call.request.nodeValue, function (result) {
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
}

async function _unsubscribeValue(call, callback) {
    console.log("unsubscribe " + call.request.subscriptionId + " (gRPC)");

    let th = valuesTracked.find(x => x.subscriptionID == call.request.subscriptionId);

    if (th == null)
        return;

    await OPC.unsubscribeValue(th.opcenv.subscriptionId);

    th.thread.end();


    callback(null, null);
}

async function _writeValue(call, callback) {
    let result = OPC.writeValue(call.request.nodeValue, call.request.value, call.request.type);

    callback(null, { response: result });
}

/**
 * Starts an RPC server that receives requests for the Greeter service at the
 * sample server port
 */
async function main() {
    // Connect to the OPC server
    await OPC.connect();


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
}

main();


//*/


