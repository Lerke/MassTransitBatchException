using MassTransit;
using Serilog;
using TestMessages;

var builder = WebApplication.CreateBuilder(args);
Log.Logger = new LoggerConfiguration()
	// .WriteTo.File("output.log")
	.WriteTo.Console()
	.CreateLogger();

builder.Logging.AddSerilog();

builder.Services.AddMassTransit(x =>
{

	x.UsingRabbitMq((ctx, cfg) =>
	{
		cfg.UseMessageRetry(r => r.None());
		cfg.Host("localhost", 45672, "/", h =>
		{
			h.Username("test");
			h.Password("test");
		});
		cfg.ConfigureEndpoints(ctx);
	});

	x.AddConsumer(typeof(BatchedConsumer), typeof(BatchedConsumerDefinition));
});

var app = builder.Build();

app.Run();

public class BatchedConsumer : IConsumer<Batch<Message>>
{
	public async Task Consume(ConsumeContext<Batch<Message>> context)
	{
		await Task.Delay(TimeSpan.FromSeconds(10));

		throw new Exception("Whoops!");
	}
}

public class BatchedConsumerDefinition : ConsumerDefinition<BatchedConsumer>
{
	public BatchedConsumerDefinition()
	{
		Endpoint(o =>
		{
			o.PrefetchCount = 1000;
		});
	}

	protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<BatchedConsumer> consumerConfigurator)
	{
		consumerConfigurator.Options<BatchOptions>(options =>
		{
			options.SetMessageLimit(1000)
				.SetTimeLimit(TimeSpan.FromMinutes(1))
				.SetConcurrencyLimit(1);
		});
	}
}

namespace TestMessages
{
	public class Message
	{
		public string Property { get; set; }
	}
}