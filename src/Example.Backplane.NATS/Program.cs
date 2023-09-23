using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.NATS;


const string cacheKey = "Example.Backplane.NATS3";

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDistributedSqlServerCache(options =>
{
	options.ConnectionString = "Data Source=localhost;Initial Catalog=DistCache;Integrated Security=True;Encrypt=false";
	options.SchemaName = "dbo";
	options.TableName = "TestCache";
});

builder.Services.AddFusionCache()
	.WithNeueccMessagePackSerializer()
	.WithDistributedCache(x => x.GetRequiredService<IDistributedCache>())
	.WithNatsBackplane(opts =>
	{
		opts.Server = "nats://localhost:4222";
		opts.Consumer = "unique-per-cache-instance";
	});
	
		
var host = builder.Build();

var cache = host.Services.GetRequiredService<IFusionCache>();


Console.WriteLine("Enter '1' to poll cache. '2' to set cache. 'q' to quit.");
var input = Console.ReadLine();

switch (input)
{
	case "1":
	{
		await cache.SetAsync(cacheKey, "Hello World!", TimeSpan.FromMinutes(5));
		while (true)
		{
			var value = await cache.GetOrDefaultAsync<string>(cacheKey);
			Console.WriteLine($"Value: {value}");

			await Task.Delay(TimeSpan.FromSeconds(5));
		}
	}
	case "2":
	{
		
		while(true)
		{
			Console.WriteLine("Enter a value to set:");
			input = Console.ReadLine();
			if (input == "q")
				break;
		
			await cache.SetAsync(cacheKey, input);
		}

		break;
	}
}
