using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PickPointTest.DataProviders;
using PickPointTest.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace PickPointTest.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PostautomatController : ControllerBase
    {
        private readonly MsSqlTestDbContext _dbContext;

        public PostautomatController(MsSqlTestDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("GetOpenedPostautomat")]
        public async IAsyncEnumerable<PostautomatJSON> GetOpenedPostautomat()
        {
            try
            {
                var collection =
                    await Task.Run(() => _dbContext.GetOpenedPostautomats());
                await foreach (var item in collection)
                    if (item != null)
                        yield return new PostautomatJSON()
                            {address = item.Address, number = item.Name, status = item.IsOpen};
            }
            finally
            {
            }
        }


        [HttpGet("GetPostautomat")]
        [SwaggerOperation(Description =
            "Request is the JSON object. Request format:  {\"number\":\"string value\"}. Value format: XXXX-XXXX")]
        [ProducesResponseType(typeof(PostautomatJSON), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPostautomat(string request)
        {
            try
            {
                var jObj = JObject.Parse(request);
                var number = jObj[$"{nameof(PostautomatJSON.number).ToLower()}"]?.ToString() ?? string.Empty;
                if (!PostautomatJSON.IsValidNumber(number)) return BadRequest("Required numbers format XXXX-XXXX");
                var postautomat = await _dbContext.FindPostautomat(number);
                if (postautomat == null) return NotFound();
                return Ok(new PostautomatJSON()
                {
                    address = postautomat.Address,
                    number = postautomat.Name,
                    status = postautomat.IsOpen,
                });
            }
            catch (JsonReaderException)
            {
                return BadRequest($"Request '{request}' is not JSON object");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}