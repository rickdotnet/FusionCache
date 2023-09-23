using ZiggyCreatures.Caching.Fusion.Serialization;

namespace ZiggyCreatures.Caching.Fusion.Backplane.NATS
{
	public class MessagePublisher: IDisposable
	{
		private readonly JetStreamManager _jetStreamManager;
		private readonly IFusionCacheSerializer _serializer;

		public MessagePublisher(JetStreamManager jetStreamManager, IFusionCacheSerializer serializer)
		{
			_jetStreamManager = jetStreamManager ?? throw new ArgumentNullException(nameof(jetStreamManager));
			_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
		}

		public async ValueTask PublishAsync(BackplaneMessage message, NatsBackplaneOptions options, CancellationToken cancellationToken = default)
		{
			var jetStream = await _jetStreamManager.GetJetStreamAsync(cancellationToken);
			await jetStream.PublishAsync(options.Subject, await _serializer.SerializeAsync(message));
		}

		public void Publish(BackplaneMessage message, NatsBackplaneOptions options)
		{
			var jetStream = _jetStreamManager.GetJetStreamAsync().GetAwaiter().GetResult();
			jetStream.Publish(options.Subject, _serializer.Serialize(message));
		}

		public void Dispose()
		{
			_jetStreamManager.Dispose();
		}
	}
}
