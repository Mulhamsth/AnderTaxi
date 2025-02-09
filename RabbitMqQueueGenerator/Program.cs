
using RabbitMQ.Client;

var factory = new ConnectionFactory { HostName = "rabbitmq" , UserName = "admin", Password = "admin" };
await using var connection = await factory.CreateConnectionAsync();
await using var model = await connection.CreateChannelAsync();

await model.ExchangeDeclareAsync(exchange: "taxi.topic", type: "topic");
await model.QueueDeclareAsync(queue: "distancelogic", durable: true, exclusive: false, autoDelete: false, arguments: null);
await model.QueueDeclareAsync(queue: "driver", durable: true, exclusive: false, autoDelete: false, arguments: null);
await model.QueueDeclareAsync(queue: "passenger", durable: true, exclusive: false, autoDelete: false, arguments: null);
await model.QueueDeclareAsync(queue: "log", durable: true, exclusive: false, autoDelete: false, arguments: null);
await model.QueueDeclareAsync(queue: "invoice", durable: true, exclusive: false, autoDelete: false, arguments: null);
await model.QueueDeclareAsync(queue: "visualizer", durable: true, exclusive: false, autoDelete: false, arguments: null);
//Taxi Queue is defined in Taxi

// Bind the queues to the exchange with the appropriate routing keys
await model.QueueBindAsync(queue: "distancelogic", exchange: "taxi.topic", routingKey: "distancelogic");
await model.QueueBindAsync(queue: "driver", exchange: "taxi.topic", routingKey: "driver"); 
await model.QueueBindAsync(queue: "passenger", exchange: "taxi.topic", routingKey: "passenger");
await model.QueueBindAsync(queue: "log", exchange: "taxi.topic", routingKey: "#");
await model.QueueBindAsync(queue: "invoice", exchange: "taxi.topic", routingKey: "invoice");
await model.QueueBindAsync(queue: "visualizer", exchange: "taxi.topic", routingKey: "#");
File.WriteAllText("/app/exitcode", "0"); // Write success
