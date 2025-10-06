using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Query.Application.Models;
using Query.Application.Services;

namespace Query.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QueryController : ControllerBase
    {
        private readonly ExecutionPlanner _executionPlanner;

        public QueryController(ExecutionPlanner executionPlanner)
            => _executionPlanner = executionPlanner ?? throw new ArgumentNullException(nameof(executionPlanner));
        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            ODataSpec spec;
            try
            {
                spec = ODataSpec.From(Request.Query as IDictionary<string, Microsoft.Extensions.Primitives.StringValues>);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }

            var result = await _executionPlanner.ExecuteAsync(spec, cancellationToken).ConfigureAwait(false);

            var payload = new Dictionary<string, object?>
            {
                ["value"] = result.Items
            };

            if (spec.Count)
            {
                payload["@odata.count"] = result.Count ?? result.Items.Count;
            }

            return Ok(payload);
        }
    }
}
