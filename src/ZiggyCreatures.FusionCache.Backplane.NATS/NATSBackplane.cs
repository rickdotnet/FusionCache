using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion.Serialization;

namespace ZiggyCreatures.Caching.Fusion.Backplane.NATS;

public class NatsBackplane : IFusionCacheBackplane, IDisposable
{
	private readonly MessagePublisher _publisher;
	private readonly MessageSubscriber _subscriber;
	
	// necessary to avoid receiving messages from itself
	private readonly string _sourceId = Guid.NewGuid().ToString("N");
	
	private ILogger<NatsBackplane>? _logger;
	private readonly NatsBackplaneOptions _backplaneOptions;
	private readonly SemaphoreSlim _connectionLock = new(initialCount: 1, maxCount: 1);

	/// <summary>
	/// Initializes a new instance of the NATSBackplane class.
	/// </summary>
	/// <param name="optionsAccessor">The set of options to use with this instance of the backplane.</param>
	/// <param name="serializer">The required serializer</param>
	/// <param name="logger">The <see cref="ILogger"/> instance to use. If null, logging will be completely disabled.</param>
	public NatsBackplane(
		IOptions<NatsBackplaneOptions> optionsAccessor,
		IFusionCacheSerializer serializer,
		ILogger<NatsBackplane>? logger = null)
	{
		if (optionsAccessor is null)
			throw new ArgumentNullException(nameof(optionsAccessor));

		_backplaneOptions = optionsAccessor.Value ?? throw new ArgumentNullException(nameof(optionsAccessor.Value));
		var jetStreamManager = new JetStreamManager(_backplaneOptions);

		// IGNORE NULL LOGGER (FOR BETTER PERF)
		// ReSharper disable once SuspiciousTypeConversion.Global
		_logger = logger is NullLogger<NatsBackplaneOptions> ? null : logger;

		_publisher = new MessagePublisher(jetStreamManager, serializer);
		_subscriber = new MessageSubscriber(_sourceId, jetStreamManager, serializer);
	}

	public async ValueTask PublishAsync(BackplaneMessage message, FusionCacheEntryOptions options,
		CancellationToken token = default)
	{
		await _publisher.PublishAsync(message, _backplaneOptions, token);
	}

	public void Publish(BackplaneMessage message, FusionCacheEntryOptions options, CancellationToken token = default)
	{
		_publisher.Publish(message, _backplaneOptions);
	}

	public void Subscribe(BackplaneSubscriptionOptions options)
	{
		_subscriber.SubscribeAsync(
			_backplaneOptions,
			options.IncomingMessageHandler 
			?? (message =>
				_logger?
					.LogWarning("IncomingMessageHandler not set. CacheKey: {CacheKey}", message.CacheKey)
			)
		);
	}

	public void Unsubscribe()
	{
		_subscriber.UnsubScribe();
	}

	public void Dispose()
	{
		_connectionLock.Dispose();
		_publisher.Dispose();
		_subscriber?.Dispose();
	}
}
