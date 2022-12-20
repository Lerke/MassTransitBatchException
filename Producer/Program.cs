using MassTransit;
using MassTransit.Transports;
using Serilog;
using TestMessages;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
	.WriteTo.Console()
	.CreateLogger();

builder.Logging.AddSerilog();
builder.Services.AddMassTransit(x =>
{
	x.UsingRabbitMq((ctx, cfg) =>
	{
		cfg.Host("localhost", 45672, "/", h =>
		{
			h.Username("test");
			h.Password("test");
		});

		cfg.ConfigureEndpoints(ctx);
	});
});

var app = builder.Build();
app.MapGet("/", () => "Hello World!");

var publish = app.Services.GetRequiredService<IPublishEndpoint>();

var messages = Enumerable.Range(0, 1000)
	.Select(x => new Message()
	{
		Property = x.ToString()
	}).ToList();

foreach (var m in messages)
{
	await publish.Publish(m, new CancellationToken());
}

app.Run();