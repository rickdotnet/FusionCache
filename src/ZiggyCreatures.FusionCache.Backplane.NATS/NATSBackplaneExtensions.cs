using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.JetStream;

namespace ZiggyCreatures.Caching.Fusion.Backplane.NATS
{
	public static class NatsBackplaneExtensions
	{
		/// <summary>
		/// Adds a NATS based implementation of a backplane to the <see cref="IFusionCacheBuilder" />.
		/// </summary>
		/// <param name="builder">The <see cref="IFusionCacheBuilder" /> to add the backplane to.</param>
		/// <param name="setupOptionsAction">The <see cref="Action{NATSBackplaneOptions}"/> to configure the provided <see cref="NatsBackplaneOptions"/>.</param>
		/// <returns>The <see cref="IFusionCacheBuilder"/> so that additional calls can be chained.</returns>
		public static IFusionCacheBuilder WithNatsBackplane(this IFusionCacheBuilder builder,
			Action<NatsBackplaneOptions>? setupOptionsAction = null)
		{
			if (builder is null)
				throw new ArgumentNullException(nameof(builder));

			return builder
					.WithBackplane(sp =>
					{
						var options = sp.GetService<IOptionsMonitor<NatsBackplaneOptions>>().Get(builder.CacheName);
						setupOptionsAction?.Invoke(options);

						var logger = sp.GetService<ILogger<NatsBackplane>>();

						// this is already be checked in the builder, safety first
						if (builder.ThrowIfMissingSerializer && builder.Serializer == null)
							throw new InvalidOperationException("Cannot use a null serializer with the NATS backplane");

						return new NatsBackplane(options, builder.Serializer!, logger);
					})
				;
		}

		public static void CreateStreamIfNotExists(this IJetStreamManagement jsm, string stream, string subject)
		{
			// in case the stream was here before, we want a completely new one
			try
			{
				jsm.GetStreamInfo(stream); // this throws if the stream does not exist
				return;
			}
			catch (Exception)
			{
				// ignored
			}

			jsm.AddStream(StreamConfiguration.Builder()
				.WithName(stream)
				.WithStorageType(StorageType.Memory)
				.WithSubjects(subject)
				.Build());
		}
	}
}
