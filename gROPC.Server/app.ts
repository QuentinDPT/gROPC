import * as config from "config";
import _ = require("lodash");

var OPCUA = require("./OPCUA");
var grpc = require("@grpc/grpc-js");
var protoLoader = require('@grpc/proto-loader');

var PROTO_PATH = __dirname + "/protos/opcua.proto";

var serverURL = config.get("client.ip") + ":" + config.get("client.port");
var OPCURL = "opc.tcp://localhost:49320";
var writeWhiteList : string[] = config.get("whitelist");
for (var n of writeWhiteList) {
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
    console.log("read recu");

    var result = await OPC.readValue(call.request.nodeValue);

    callback(null, { response: result });
}

var observerID = 1;

var valuesTracked = [];


async function _subscribeValue(call, callback) {
    let subid = observerID++;

    console.log("sub recu");

    let _opcenv = await OPC.subscribeValue(call.request.nodeValue, function (result) {
        call.write({
            subsciptionId: subid,
            response: result
        });
    });

    console.log("subscription on " + subid);

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
    console.log("ask for unsubscription to " + call.request.subscriptionId);

    var th = valuesTracked.find(x => x.subscriptionID == call.request.subscriptionId);

    if (th == null)
        return;

    await OPC.unsubscribeValue(th.opcenv.subscriptionId);

    th.thread.end();


    callback(null, null);
}

async function _writeValue(call, callback) {
    console.log("write value");

    var rezz = OPC.writeValue(call.request.nodeValue, call.request.value, call.request.type);

    callback(null, { response: rezz });
}

/**
 * Starts an RPC server that receives requests for the Greeter service at the
 * sample server port
 */
async function main() {
    // Connect to the OPC server
    await OPC.connect();


    // Starting the gRPC server
    var server = new grpc.Server();
    server.addService(opcuaProto.OPCUAServices.service, {
        readValue: _readValue,
        subscribeValue: _subscribeValue,
        unsubscribeValue: _unsubscribeValue,
        writeValue: _writeValue
    });
    server.bindAsync('0.0.0.0:50000', grpc.ServerCredentials.createInsecure(), () => {
        server.start();
    });
}

main();


//*/


