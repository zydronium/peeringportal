using LUJEWebsite.Library.Models;
using LUJEWebsite.PeeringApi.Attributes;
using LUJEWebsite.PeeringApi.Models;
using LUJEWebsite.PeeringApi.Payload;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace LUJEWebsite.PeeringApi.Controllers
{
    [ApiController]
    [Route("v0.1")]
    public class PeeringController : Controller
    {
        /// <summary>
        /// 
        /// </summary>
        /// <response code="200">OK</response>
        /// <response code="400">API Error response</response>
        [HttpGet]
        [Route("/v0.1/auth")]
        [ValidateModelState]
        [SwaggerOperation("AuthGet")]
        [SwaggerResponse(statusCode: 400, type: typeof(AuthGet400Response), description: "API Error response")]
        public virtual IActionResult AuthGet()
        {

            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200);
            //TODO: Uncomment the next line to return response 400 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(400, default);

            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>Lists all the visible locations to the caller.</remarks>
        /// <param name="asn">List available locations for peering with the given ASN</param>
        /// <param name="locationType">Optional filter for only querying locations able to establish public or private connections.</param>
        /// <param name="maxResults">Hint to paginate the request with the maximum number of results the caller is able to process.</param>
        /// <param name="nextToken">Indication of the offset to retrieve the next page on a previously initiated paginated request.</param>
        /// <response code="200">OK</response>
        /// <response code="400">API Error response</response>
        /// <response code="403">API Error response</response>
        [HttpGet]
        [Route("/v0.1/locations")]
        [ValidateModelState]
        [SwaggerOperation("LocationsGet")]
        [SwaggerResponse(statusCode: 200, type: typeof(ListLocationsResponse), description: "OK")]
        [SwaggerResponse(statusCode: 400, type: typeof(AuthGet400Response), description: "API Error response")]
        [SwaggerResponse(statusCode: 403, type: typeof(AuthGet400Response), description: "API Error response")]
        public virtual IActionResult LocationsGet([FromQuery(Name = "asn")][Required()] int asn, [FromQuery(Name = "location_type")] LocationType? locationType, [FromQuery(Name = "max_results")][Range(0, 100)] int? maxResults, [FromQuery(Name = "next_token")] string nextToken)
        {

            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default);
            //TODO: Uncomment the next line to return response 400 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(400, default);
            //TODO: Uncomment the next line to return response 403 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(403, default);
            string exampleJson = null;
            exampleJson = "";
            exampleJson = "{\r\n  \"errors\" : {\r\n    \"errors\" : [ {\r\n      \"name\" : \"name\",\r\n      \"errors\" : [ \"errors\", \"errors\" ]\r\n    }, {\r\n      \"name\" : \"name\",\r\n      \"errors\" : [ \"errors\", \"errors\" ]\r\n    } ]\r\n  }\r\n}";
            exampleJson = "{\r\n  \"errors\" : {\r\n    \"errors\" : [ {\r\n      \"name\" : \"name\",\r\n      \"errors\" : [ \"errors\", \"errors\" ]\r\n    }, {\r\n      \"name\" : \"name\",\r\n      \"errors\" : [ \"errors\", \"errors\" ]\r\n    } ]\r\n  }\r\n}";

            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<ListLocationsResponse>(exampleJson)
            : default;
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>Requests to turn the session down and delete it from the server.</remarks>
		/// <param name="requestBody">Request to delete peering sessions</param>
		/// <response code="200">Sessions are scheduled for deletion and no longer usable</response>
		/// <response code="400">API Error response</response>
		/// <response code="403">API Error response</response>
		/// <response code="422">API Error response</response>
		[HttpDelete]
        [Route("/v0.1/sessions")]
        [Consumes("application/json")]
        [ValidateModelState]
        [SwaggerOperation("SessionsDelete")]
        [SwaggerResponse(statusCode: 200, type: typeof(DeleteSessionsResponse), description: "Sessions are scheduled for deletion and no longer usable")]
        [SwaggerResponse(statusCode: 400, type: typeof(AuthGet400Response), description: "API Error response")]
        [SwaggerResponse(statusCode: 403, type: typeof(AuthGet400Response), description: "API Error response")]
		[SwaggerResponse(statusCode: 422, type: typeof(AuthGet400Response), description: "API Error response")]
		public virtual IActionResult SessionsDelete([FromBody] List<string> requestBody)
        {

			return StatusCode(422, default);

			//TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
			// return StatusCode(200, default);
			//TODO: Uncomment the next line to return response 400 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
			// return StatusCode(400, default);
			//TODO: Uncomment the next line to return response 403 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
			// return StatusCode(403, default);
			/*string exampleJson = null;
            exampleJson = "";
            exampleJson = "{\r\n  \"errors\" : {\r\n    \"errors\" : [ {\r\n      \"name\" : \"name\",\r\n      \"errors\" : [ \"errors\", \"errors\" ]\r\n    }, {\r\n      \"name\" : \"name\",\r\n      \"errors\" : [ \"errors\", \"errors\" ]\r\n    } ]\r\n  }\r\n}";
            exampleJson = "{\r\n  \"errors\" : {\r\n    \"errors\" : [ {\r\n      \"name\" : \"name\",\r\n      \"errors\" : [ \"errors\", \"errors\" ]\r\n    }, {\r\n      \"name\" : \"name\",\r\n      \"errors\" : [ \"errors\", \"errors\" ]\r\n    } ]\r\n  }\r\n}";

            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<DeleteSessionsResponse>(exampleJson)
            : default;
            //TODO: Change the data returned
            return new ObjectResult(example);*/
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>Lists a set of sessions visible to the caller. Each session in the set contains a summary of its current status.</remarks>
        /// <param name="asn">asn of requester</param>
        /// <param name="maxResults">Hint to paginate the request with the maximum number of results the caller is able to process.</param>
        /// <param name="nextToken">Indication of the offset to retrieve the next page on a previously initiated paginated request.</param>
        /// <response code="200">OK</response>
        /// <response code="400">API Error response</response>
        /// <response code="403">API Error response</response>
        [HttpGet]
        [Route("/v0.1/sessions")]
        [ValidateModelState]
        [SwaggerOperation("SessionsGet")]
        [SwaggerResponse(statusCode: 200, type: typeof(ListSessionsResponse), description: "OK")]
        [SwaggerResponse(statusCode: 400, type: typeof(AuthGet400Response), description: "API Error response")]
        [SwaggerResponse(statusCode: 403, type: typeof(AuthGet400Response), description: "API Error response")]
        public virtual async Task<IActionResult> SessionsGetAsync([FromQuery(Name = "asn")][Required()] int asn, [FromQuery(Name = "max_results")][Range(0, 100)] int? maxResults, [FromQuery(Name = "next_token")] string? nextToken)
        {
			ListSessionsResponse response = await NetworkPage.NetworkPageAsync(asn, maxResults, nextToken);
            return new ObjectResult(response);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>Requests a new set of sessions to be created.</remarks>
        /// <param name="session">Request to create peering sessions</param>
        /// <response code="200">OK</response>
        /// <response code="400">API Error response</response>
        /// <response code="403">API Error response</response>
        [HttpPost]
        [Route("/v0.1/sessions")]
        [Consumes("application/json")]
        [ValidateModelState]
        [SwaggerOperation("SessionsPost")]
        [SwaggerResponse(statusCode: 200, type: typeof(CreateSessionsResponse), description: "OK")]
        [SwaggerResponse(statusCode: 400, type: typeof(AuthGet400Response), description: "API Error response")]
        [SwaggerResponse(statusCode: 403, type: typeof(AuthGet400Response), description: "API Error response")]
        public virtual IActionResult SessionsPost([FromBody] List<Session> session)
        {

            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default);
            //TODO: Uncomment the next line to return response 400 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(400, default);
            //TODO: Uncomment the next line to return response 403 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(403, default);
            string exampleJson = null;
            exampleJson = "";
            exampleJson = "{\r\n  \"errors\" : {\r\n    \"errors\" : [ {\r\n      \"name\" : \"name\",\r\n      \"errors\" : [ \"errors\", \"errors\" ]\r\n    }, {\r\n      \"name\" : \"name\",\r\n      \"errors\" : [ \"errors\", \"errors\" ]\r\n    } ]\r\n  }\r\n}";
            exampleJson = "{\r\n  \"errors\" : {\r\n    \"errors\" : [ {\r\n      \"name\" : \"name\",\r\n      \"errors\" : [ \"errors\", \"errors\" ]\r\n    }, {\r\n      \"name\" : \"name\",\r\n      \"errors\" : [ \"errors\", \"errors\" ]\r\n    } ]\r\n  }\r\n}";

            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<CreateSessionsResponse>(exampleJson)
            : default;
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>Requests to turn the session down and delete it from the server.</remarks>
		/// <response code="200">Session is scheduled for deletion and no longer usable</response>
		/// <response code="400">API Error response</response>
		/// <response code="403">API Error response</response>
		/// <response code="404">API Error response</response>
		/// <response code="422">API Error response</response>
		[HttpDelete]
        [Route("/v0.1/sessions/{session_id}")]
        [ValidateModelState]
        [SwaggerOperation("SessionsSessionIdDelete")]
        [SwaggerResponse(statusCode: 200, type: typeof(Session), description: "Session is scheduled for deletion and no longer usable")]
        [SwaggerResponse(statusCode: 400, type: typeof(AuthGet400Response), description: "API Error response")]
        [SwaggerResponse(statusCode: 403, type: typeof(AuthGet400Response), description: "API Error response")]
        [SwaggerResponse(statusCode: 404, type: typeof(AuthGet400Response), description: "API Error response")]
		[SwaggerResponse(statusCode: 422, type: typeof(AuthGet400Response), description: "API Error response")]
		public virtual async Task<IActionResult> SessionsSessionIdDeleteAsync([FromRoute(Name = "session_id")][Required] string sessionId)
        {

			string[] SessionArray = sessionId.Split('-');
			string asn = SessionArray[0];
			string ixlan_id = SessionArray[1];
			string id = SessionArray[2];
			string ownerid = SessionArray[3];
			string afi = SessionArray[4];

			Session response = await SessionPage.SessionPageAsync(asn, ixlan_id, id, ownerid, afi);
			if (response == null)
			{
				return StatusCode(404, default);
            }
            else
            {
				return StatusCode(422, default);
			}
		}

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>Retrieves a given session by it server-generated ID provided on creation.</remarks>
        /// <param name="sessionId">Server-generated stable identifier for the session.</param>
        /// <response code="200">OK</response>
        /// <response code="400">API Error response</response>
        /// <response code="403">API Error response</response>
        /// <response code="404">API Error response</response>
        [HttpGet]
        [Route("/v0.1/sessions/{session_id}")]
        [ValidateModelState]
        [SwaggerOperation("SessionsSessionIdGet")]
        [SwaggerResponse(statusCode: 200, type: typeof(Session), description: "OK")]
        [SwaggerResponse(statusCode: 400, type: typeof(AuthGet400Response), description: "API Error response")]
        [SwaggerResponse(statusCode: 403, type: typeof(AuthGet400Response), description: "API Error response")]
        [SwaggerResponse(statusCode: 404, type: typeof(AuthGet400Response), description: "API Error response")]
        public virtual async Task<IActionResult> SessionsSessionIdGetAsync([FromRoute(Name = "session_id")][Required] string sessionId)
        {
            string[] SessionArray = sessionId.Split('-');
            string asn = SessionArray[0];
            string ixlan_id = SessionArray[1];
            string id = SessionArray[2];
			string ownerid = SessionArray[3];
			string afi = SessionArray[4];

			Session response = await SessionPage.SessionPageAsync(asn, ixlan_id, id, ownerid, afi);
            if (response == null)
            {
				return StatusCode(404, default);
			}
			return new ObjectResult(response);
		}
    }
}
