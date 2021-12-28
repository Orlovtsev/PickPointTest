#define TEST
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PickPointTest.DataProviders;
using PickPointTest.DataProviders.DataModels;
using PickPointTest.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace PickPointTest.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly MsSqlTestDbContext _dbContext;

        public OrderController(MsSqlTestDbContext dbContext)
        {
            _dbContext = dbContext;
        }

#if TEST
        [HttpPost("SetTestDATA")]
        public void PostStartedData()
        {
            _dbContext._addTestData();
        }
#endif


        [HttpGet("GetOrder")]
        [SwaggerOperation(Description = "Request is the JSON object. Format: {\"number\":\"int value\"}")]
        [ProducesResponseType(typeof(OrderJSON), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrder(string request)
        {
            try
            {
                var order = await _getOrderData(request);
                if (order == null) return NotFound();
                return Ok(_from(order));
            }
            catch (BadRequestException e)
            {
                return BadRequest(e.Message);
            }
            catch (NotFoundDataException)
            {
                return NotFound();
            }
            catch (JsonReaderException)
            {
                return BadRequest($"Request '{request}' is not JSON object");
            }
            catch (Exception e)
            {
                return Problem(detail: $"{e.Message}\n\n{e.StackTrace}");
            }
        }

        private async Task<OrderData> _getOrderData(string request)
        {
            if (string.IsNullOrWhiteSpace(request)) throw new BadRequestException("Empty request");
            var jObj = JObject.Parse(request);
            var isNumber = int.TryParse(jObj[$"{nameof(OrderJSON.number).ToLower()}"]?.ToString() ?? string.Empty,
                out var number);
            if (!isNumber) throw new BadRequestException("Not valid number value");
            var order = await _dbContext.FindOrder(number);
            if (order == null) throw new NotFoundDataException();
            return order;
        }

        private static OrderJSON _from(OrderData order)
        {
            return new OrderJSON()
            {
                number = order.Id,
                composition = order.Products.Select(x => x.Product.Name).ToArray(),
                cost = order.Cost,
                phone = order.RecipientPhone,
                postautomat = order.Postautomat.Name,
                recipient = order.RecipientName,
                status = order.OrderStatus.Id
            };
        }


        [HttpPost("PostOrder")]
        [ProducesResponseType(typeof(OrderJSON), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(Description = "Request is the JSON object.\n" +
                                        "See more information to OrderJSON Schema" +
                                        "Status range [1..6]")]
        public async Task<IActionResult> PostOrder(string request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request)) throw new BadRequestException("Empty request");
                var order = JsonConvert.DeserializeObject<OrderJSON>(request);
                if (order.composition.Length > 10)
                    throw new BadRequestException("OrderJSON does not contains more than 10 products");
                var orderData = await _dbContext.FindOrder(order.number);
                if (orderData != null) throw new BadRequestException("The order exists");
                orderData = new OrderData()
                {
                    Id = order.number,
                    RecipientName = order.recipient,
                };
                if (!order.IsValidNumber(out var error)) throw new BadRequestException(error);
                orderData.RecipientPhone = order.phone;
                var products = _dbContext.GetProducts(order.composition);
                var orderProducts = products.Select(
                    x => new OrderProductData() {ProductId = x.ID, OrderId = orderData.Id}).ToList();
                orderData.Cost = order.cost;
                orderData.Products = orderProducts;
                var postautomat = await _dbContext.FindPostautomat(order.postautomat);
                if (postautomat == null) throw new BadRequestException("Postautomat not found");
                orderData.Postautomat = postautomat;
                var status = await _dbContext.FindStatus(order.status);
                if (status == null) throw new BadRequestException("Status not found");
                orderData.OrderStatus = status;
                var contextStatus = _dbContext.Insert(orderData);
                if (contextStatus != 0) return Created(nameof(PostOrder), _from(orderData));
                _dbContext.Delete(orderProducts);
                _dbContext.Delete(orderData);
                return Problem("Order not posted");
            }
            catch (BadRequestException e)
            {
                return BadRequest(e.Message);
            }
            catch (NotFoundDataException)
            {
                return NotFound();
            }
            catch (JsonReaderException)
            {
                return BadRequest($"Request '{request}' is not JSON object");
            }
            catch (Exception e)
            {
                return Problem($"{e.Message}\n\n{e.StackTrace}");
            }
        }


        [HttpPut("ChangeStatus")]
        [SwaggerOperation(Description = "Request is the JSON object.\n" +
                                        "Format: {\"number\":int value,\"status\":int value}." +
                                        "Status range = [1..6]")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangeStatus(string request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request)) throw new BadRequestException("Empty request");
                var jObj = JObject.Parse(request);
                var isNumber = int.TryParse(jObj[$"{nameof(OrderJSON.number).ToLower()}"]?.ToString() ?? string.Empty,
                    out var number);
                if (!isNumber) throw new BadRequestException("Not valid number value");
                isNumber = int.TryParse(jObj[$"{nameof(OrderJSON.status).ToLower()}"]?.ToString() ?? string.Empty,
                    out var statusId);
                if (!isNumber) throw new BadRequestException("Not valid status value");
                var order = await _dbContext.FindOrder(number);
                if (order == null) throw new NotFoundDataException();
                var status = await _dbContext.ChangeStatus(number, statusId);
                if (status == 0)
                {
                    return Problem("Failed to save changes");
                }

                var savedOrder = await Task.Run(() => _dbContext.FindOrder(number));
                return Ok(_from(savedOrder));
            }
            catch (BadRequestException e)
            {
                return BadRequest(e.Message);
            }
            catch (NotFoundDataException)
            {
                return NotFound();
            }
            catch (JsonReaderException)
            {
                return BadRequest($"Request '{request}' is not JSON object");
            }
            catch (Exception e)
            {
                return Problem($"{e.Message}\n\n{e.StackTrace}");
            }
        }

        [HttpPut("ChangeProductComposition")]
        [SwaggerOperation(Description = "Request is the JSON object.\n" +
                                        "Format: {\"number\":int value,\"composition\":string[],\"cost\":\"500\"}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangeProductComposition(string request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request)) throw new BadRequestException("Empty request");
                var jObj = JObject.Parse(request);
                var isNumber = int.TryParse(jObj[$"{nameof(OrderJSON.number).ToLower()}"]?.ToString() ?? string.Empty,
                    out var number);
                if (!isNumber) throw new BadRequestException("Not valid number value");
                var compositionObj =  jObj[$"{nameof(OrderJSON.composition)}"]?.ToObject(typeof(string[])) ?? throw new BadRequestException("Not valid composition value");
                var composition = (string[]) compositionObj;
                var costObj = jObj[$"{nameof(OrderJSON.cost)}"]?.ToObject(typeof(decimal)) ??
                              throw new BadRequestException("Not valid cost value");
                var cost = (decimal) costObj;
                var orderData = await _dbContext.FindOrder(number);
                if (orderData == null) throw new NotFoundDataException();
                var status = await _dbContext.ChangeProducts(number, composition,cost);
                if (status == 0)
                {
                    return Problem("Failed to save changes");
                }

                var savedOrder = await Task.Run(() => _dbContext.FindOrder(number));
                return Ok(_from(savedOrder));
            }
            catch (BadRequestException e)
            {
                return BadRequest(e.Message);
            }
            catch (NotFoundDataException)
            {
                return NotFound();
            }
            catch (JsonReaderException)
            {
                return BadRequest($"Request '{request}' is not JSON object");
            }
            catch (Exception e)
            {
                return Problem($"{e.Message}\n\n{e.StackTrace}");
            }
        }


        [HttpDelete("DeleteOrder")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOrder(string request)
        {
            try
            {
                var order = await _getOrderData(request);
                var changeStatus = _dbContext.Delete(order);
                if (changeStatus == 0) return Problem("Order not deleted");
                return NoContent();
            }
            catch (BadRequestException e)
            {
                return BadRequest(e.Message);
            }
            catch (NotFoundDataException)
            {
                return NotFound();
            }
            catch (JsonReaderException)
            {
                return BadRequest($"Request '{request}' is not JSON object");
            }
            catch (Exception e)
            {
                return Problem($"{e.Message}\n\n{e.StackTrace}");
            }
        }
    }
}