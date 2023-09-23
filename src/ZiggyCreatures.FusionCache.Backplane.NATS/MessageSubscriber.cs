using NATS.Client;
using NATS.Client.JetStream;
using ZiggyCreatures.Caching.Fusion.Serialization;

namespace ZiggyCreatures.Caching.Fusion.Backplane.NATS
{
	public class MessageSubscriber : IDisposable
	{
		private readonly string _sourceId;
		private readonly JetStreamManager _jetStreamManager;
		private readonly IFusionCacheSerializer _serializer;
		private IJetStreamPushAsyncSubscription? _subscription;

		public MessageSubscriber(string sourceId, JetStreamManager connectionManager, IFusionCacheSerializer serializer)
		{
			_sourceId = sourceId;
			_jetStreamManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
			_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
		}

		public async Task SubscribeAsync(NatsBackplaneOptions options, Action<BackplaneMessage> messageHandler,
			CancellationToken cancellationToken = default)
		{
			var jetStream = await _jetStreamManager.GetJetStreamAsync(cancellationToken);
			var config = new ConsumerConfiguration.ConsumerConfigurationBuilder()
				.WithName(options.Consumer)
				.WithDeliverSubject(options.Consumer)
				.WithDeliverPolicy(DeliverPolicy.New)
				.WithAckPolicy(AckPolicy.None)
				.WithAckWait(10000)
				.Build();

			var pushSubscribeOptions = new PushSubscribeOptions.PushSubscribeOptionsBuilder()
				.WithConfiguration(config)
				.Build();

			_subscription = jetStream.PushSubscribeAsync(options.Subject, HandleMessage(messageHandler), false,
				pushSubscribeOptions);
		}

		public void UnsubScribe() => _subscription?.Unsubscribe();
		
		private EventHandler<MsgHandlerEventArgs> HandleMessage(Action<BackplaneMessage> messageHandler)
		{
			return (_, args) =>
			{
				var message = _serializer.Deserialize<BackplaneMessage>(args.Message.Data);
				if (message == null)
					return;

				if (message.SourceId == _sourceId)
				{
					args.Message.Ack();
					return;
				}

				Console.WriteLine("Received message from NATS backplane");
				messageHandler(message);
				args.Message.Ack();
			};
		}

		

		public void Dispose()
		{
			_subscription?.Unsubscribe();
			_subscription?.Dispose();
		}
	}
}
