using Microsoft.Extensions.Options;
using NATS.Client;
using NATS.Client.JetStream;

namespace ZiggyCreatures.Caching.Fusion.Backplane.NATS
{
	public class JetStreamManager : IDisposable
	{
		private readonly NatsBackplaneOptions _options;
		private IConnection? _connection;
		private IJetStream? _jetStream;
		private readonly SemaphoreSlim _connectionLock = new(1, 1);

		public JetStreamManager(IOptions<NatsBackplaneOptions> options)
		{
			_options = options.Value ?? throw new ArgumentNullException(nameof(options.Value));
		}

		public async ValueTask<IJetStream> GetJetStreamAsync(CancellationToken cancellationToken = default)
		{
			if (_connection == null || _jetStream == null)
			{
				await _connectionLock.WaitAsync(cancellationToken);

				if (_connection == null)
				{
					var opts = ConnectionFactory.GetDefaultOptions();
					opts.Url = _options.Server;
					opts.Timeout = (int)TimeSpan.FromSeconds(10).TotalMilliseconds; // TODO: configure
					opts.AllowReconnect = true;

					if (_options.Creds != null)
					{
						opts.SetUserCredentials(_options.Creds);
					}

					_connection = new ConnectionFactory().CreateConnection(opts);
				}

				_jetStream ??= _connection.CreateJetStreamContext();

				_connectionLock.Release();
			}

			return _jetStream;
		}

		public void Dispose()
		{
			_connectionLock.Dispose();
			_connection?.Drain();
			_connection?.Dispose();
		}
	}
}
