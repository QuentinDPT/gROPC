import * as config from "config";
import _ = require("lodash");
import { v4 as uuidv4 } from 'uuid';

// Loading dependencies
const { createLogger, format, transports } = require('winston');
var OPCUA = require("./OPCUA");
var grpc = require("@grpc/grpc-js");
var protoLoader = require('@grpc/proto-loader');
const { combine, timestamp, label, printf } = format;

var subscriptionResultSeparator = "<#.#>";

// formatting the logger result
const myFormat = printf(({ level, message, service, timestamp }) => {
    return `${timestamp} [${service}] ${level}: ${message}`;
});

/// initialize the logger functionnalities
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
        new transports.File({ filename: 'error.log', level: 'warn' }),
        new transports.File({ filename: 'combined.log' }),
        new transports.Console()
    ],
});

var PROTO_PATH = __dirname + "/protos/opcua.proto";

// setting up the OPC properties
var serverURL = config.get("server.ip") + ":" + config.get("server.port");
var OPCURL = config.get("OPC.url");
// getting the whitelisted values for wrinting data
var writeWhiteList : string[] = config.get("whitelist");
for (let n of writeWhiteList) {
    OPCUA.OPCUA_whitelist.push(n.toUpperCase());
}

// load and build proto function
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

/// global variable to access to the OPC functionnalities
var OPC = new OPCUA.OPCUA(OPCURL);
OPC._logger = logger;

/**
 * Read a value form the OPC
 * 
 * @param call parameters to read a value
 * @param callback function called to end the request
 */
async function _readValue(call, callback) {
    let result = await OPC.readValue(call.request.nodeValue);

    callback(null, { response: result });
}

/// List of all subscribtion tracked
var valuesTracked = [];

/**
 * Subscribe to a value on the OPC
 * 
 * @param call parameters to subscribe to a value
 * @param callback function called to end the request
 */
async function _subscribeValue(call, callback) {
    let subid = "";
    let returnValuesAssociated = [];

    // create the GUID
    do {
        subid = uuidv4();
    } while (valuesTracked.find(x => x.subscriptionID == subid) != null);

    // getting the list of values associated
    returnValuesAssociated = call.request.returnedValues.split(subscriptionResultSeparator);

    let throwNodeNameError = function (nodeName: string) {
        call.write({
            subsciptionId: -1,
            response: nodeName
        });
    }

    // Testing if all values are existing on the OPC
    if (!await OPC.isValid(call.request.nodeValue)) {
        throwNodeNameError(call.request.nodeValue);
        return;
    }

    for (let i of returnValuesAssociated) {
        if (!await OPC.isValid(i)) {
            throwNodeNameError(i);
            return;
        }
    }

    // the first response will be the ID of the listener
    call.write({
        subsciptionId: subid,
        response: ""
    });


    // then we subscribe to the OPC server
    let stillOnline = true;
    let _opcenv = await OPC.subscribeValue(call.request.nodeValue, async function (result) {
        // if the client has disconnected and the server hasnt unsubscribe it yet
        if (!stillOnline)
            return;

        let associatedValuesPromises = [];
        let associatedValues = [];
        let responseString = result;

        if (returnValuesAssociated.length != 0) {

            // sending requests for additionnal information
            for (let i of returnValuesAssociated) {
                associatedValuesPromises.push(OPC.readValue(i));
            }

            // getting additionnal information
            await Promise.all(associatedValuesPromises).then((values) => {
                associatedValues = values;
            });

            responseString += subscriptionResultSeparator + associatedValues.join(subscriptionResultSeparator);
        }

        // send to the client the information
        stillOnline = call.write({
            subsciptionId: subid,
            response: responseString
        });

        // if the client has disconnected
        if (!stillOnline) {
            // then unsubscribe the OPC listener
            let subId = _opcenv.subscriptionId;
            logger.warn("client disconnected, close " + subId + " (OPCUA)");
            await OPC.unsubscribeValue(subId);
        }
    });

    logger.info("new subscription " + subid + " (gRPC)");

    // register the subscription on the server list
    valuesTracked.push({
        nodeValue: call.request.nodeValue,
        subscriptionID: subid,
        clientEndpoint: call.getPeer(),
        thread: call,
        opcenv: _opcenv
    });
}

/**
 * Unsubscribe a value on the OPC
 * 
 * @param call parameters to unsubscribe
 * @param callback function called to end the request
 */
async function _unsubscribeValue(call, callback) {
    logger.info("unsubscribe " + call.request.subscriptionId + " (gRPC)");

    // translating the GUID to get the subscrption
    let th = valuesTracked.find(x => x.subscriptionID == call.request.subscriptionId);

    // if the subsciption dosent exist
    if (th == null)
        return;

    // compare the client endpoint to see if he can end this subscription
    if (th.clientEndpoint != call.getPeer())
        return;

    // unsubscribe the listener from the OPC
    await OPC.unsubscribeValue(th.opcenv.subscriptionId);

    th.thread.end();

    callback(null, null);
}

/**
 * write a value on the OPC
 * 
 * @param call parameters of the request
 * @param callback function called to end the request
 */
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

// Launch the server
main();
