using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

// Read config from environment, fall back to sensible defaults
string host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
int port   = int.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_PORT"), out var p) ? p : 5673;
string user = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest";
string pass = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "guest";

var factory = new ConnectionFactory
{
    HostName = host,
    Port     = port,
    UserName = user,
    Password = pass,
    AutomaticRecoveryEnabled = true
};

var maxAttempts = 12;
var delay       = TimeSpan.FromSeconds(5);

IConnection? connection = null;
for (var attempt = 1; attempt <= maxAttempts; attempt++)
{
    try
    {
        connection = factory.CreateConnection();
        Console.WriteLine($"Connected to RabbitMQ on attempt {attempt}.");
        break;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"RabbitMQ not ready (attempt {attempt}/{maxAttempts}): {ex.Message}");
        if (attempt == maxAttempts)
            throw;
        Thread.Sleep(delay);
    }
}

using var channel = connection!.CreateModel();

const string exchange = "amq.topic";
const string queueName = "pgdemo.cdc";
const string routingKey = "pgdemo.debezium";   // must match Debezium config

channel.ExchangeDeclare(exchange: exchange,
                        type: ExchangeType.Topic,
                        durable: true);

channel.QueueDeclare(queue: queueName,
                     durable: false,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);

channel.QueueBind(queue: queueName,
                  exchange: exchange,
                  routingKey: routingKey);

var consumer = new EventingBasicConsumer(channel);
consumer.Received += (_, ea) =>
{
    var message = Encoding.UTF8.GetString(ea.Body.ToArray());
    Console.WriteLine($" [x] Received: {message}");
};

channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

Console.WriteLine("Consumer is up. Waiting for messages. Press Ctrl+C to exit.");

var shutdown = new ManualResetEventSlim(false);
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;   // Let us clean up gracefully
    shutdown.Set();
};

shutdown.Wait();       // Block indefinitely
