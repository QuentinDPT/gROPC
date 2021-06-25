var PROTO_PATH = __dirname + '/protos/opcua.proto';

var parseArgs = require('minimist');
var grpc = require('@grpc/grpc-js');
var protoLoader = require('@grpc/proto-loader');
var packageDefinition = protoLoader.loadSync(
    PROTO_PATH,
    {
        keepCase: true,
        longs: String,
        enums: String,
        defaults: true,
        oneofs: true
    });
var opcuaProto = grpc.loadPackageDefinition(packageDefinition).OPCUA;

function main() {
    var argv = parseArgs(process.argv.slice(2), {
        string: 'target'
    });
    var target;
    if (argv.target) {
        target = argv.target;
    } else {
        target = 'localhost:50000';
    }
    var client = new opcuaProto.OPCUAServices(target,
        grpc.credentials.createInsecure());
    var user;
    if (argv._.length > 0) {
        user = argv._[0];
    } else {
        user = 'nodevalue';
    }

    client.readValue({ nodeValue: "READ VALUE (node value)" }, function (err, response) {
        console.log('readValue:', response.response);
    });

    client.subscribeValue({ nodeValue: "SUBSCRIBE VALUE (node value)" }, function (err, response) {
        console.log('readValue:', response.response);
    });
}

main();
