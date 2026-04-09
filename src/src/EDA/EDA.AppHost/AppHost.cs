var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var callcenterDb = postgres.AddDatabase("callcenter");
var rabbitmq = builder.AddRabbitMQ("messaging");

var server = builder.AddProject<Projects.EDA_Server>("server")
    .WithReference(callcenterDb)
    .WithReference(rabbitmq)
    .WaitFor(callcenterDb)
    .WaitFor(rabbitmq)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithReference(server)
    .WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
