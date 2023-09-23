using Microsoft.Extensions.Options;

namespace ZiggyCreatures.Caching.Fusion.Backplane.NATS
{
	/// <summary>
	/// Represents the options available for the NATS backplane.
	/// </summary>
	public class NatsBackplaneOptions
		: IOptions<NatsBackplaneOptions>
	{
		/// <summary>
		/// The configuration used to connect to NATS.
		/// </summary>
		public string? Server { get; set; }
		public string? Creds { get; set; }
		public string Subject { get; set; } = "FusionCache-subject";
		public string Consumer { get; set; } = "FusionCache-consumer";
		
		/// <summary>
		/// The configuration used to connect to NATS.
		/// This is preferred over Configuration.
		/// </summary>
		//public ConfigurationOptions? ConfigurationOptions { get; set; }
		NatsBackplaneOptions IOptions<NatsBackplaneOptions>.Value
		{
			get { return this; }
		}
	}

}
