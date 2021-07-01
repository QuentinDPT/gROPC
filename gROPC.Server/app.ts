import * as config from "config";
import _ = require("lodash");

const { createLogger, format, transports } = require('winston');
var OPCUA = require("./OPCUA");
var grpc = require("@grpc/grpc-js");
var protoLoader = require('@grpc/proto-loader');
const { combine, timestamp, label, printf } = format;

const myFormat = printf(({ level, message, service, timestamp }) => {
    return `${timestamp} [${service}] ${level}: ${message}`;
});

const logger = createLogger({
    level: 'info',
    format: combine(
        timestamp(),
        myFormat
    ),
    defaultMeta: { service: 'gROPC' },
    transports: [
        //
        // - Write all logs with level `error` and below to `error.log`
        // - Write all logs with level `info` and below to `combined.log`
        //
        new transports.File({ filename: 'error.log', level: 'error' }),
        new transports.File({ filename: 'combined.log' }),
        new transports.Console()
    ],
});

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
OPC._logger = logger;


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

    logger.info("new subscription " + subid + " (gRPC)");

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
    logger.info("unsubscribe " + call.request.subscriptionId + " (gRPC)");

    let th = valuesTracked.find(x => x.subscriptionID == call.request.subscriptionId);

    if (th == null)
        return;

    await OPC.unsubscribeValue(th.opcenv.subscriptionId);

    th.thread.end();


    callback(null, null);
}

async function _writeValue(call, callback) {
    let result = await OPC.writeValue(call.request.nodeValue, call.request.value, call.request.type);

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


