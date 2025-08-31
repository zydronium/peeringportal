using System.Text.Json.Serialization;

namespace LUJEWebsite.PeeringApi.Models
{
    public class ListSessionsResponse
	{
		[JsonPropertyName("next_token")]
		public string? NextToken { get; set; }

		[JsonPropertyName("sessions")]
		public List<Session> Sessions { get; set; }
    }
}
