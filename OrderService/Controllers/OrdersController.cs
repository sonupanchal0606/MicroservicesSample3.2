using MassTransit;
using MassTransit.Transports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using System.Net.Http;

namespace OrderService.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class OrdersController : ControllerBase
	{

		private readonly OrderDbContext _context;
		private readonly IHttpClientFactory _httpClientFactory;
		// private readonly HttpClient _httpClient; to use http client from appsettings.json

		public OrdersController(OrderDbContext context, IHttpClientFactory httpClientFactory)
		{
			_context = context;
			_httpClientFactory = httpClientFactory;

			// _httpClient = httpClientFactory.CreateClient("ProductService");  to use http client from appsettings.json
		}

		/*		[HttpGet]
				public async Task<IActionResult> GetAll() => Ok(await _context.Orders.ToListAsync());*/

		[HttpGet]
		public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetOrders()
		{
			var orders = await _context.Orders.ToListAsync();
			var response = new List<OrderResponseDto>();
			using var client = _httpClientFactory.CreateClient("ProductService");

			foreach (var order in orders)
			{
				// Call ProductService for product info
				var product = await client.GetFromJsonAsync<ProductDto>($"https://localhost:5001/api/products/{order.ProductId}");

				// Call ProductService for product info
				// var product = await _httpClient.GetFromJsonAsync<ProductDto>($"api/products/{order.ProductId}"); //  to use http client from appsettings.json

				response.Add(new OrderResponseDto
				{
					Id = order.Id,
					Quantity = order.Quantity,
					TotalPrice = order.TotalPrice,
					Product = product
				});
			}

			return Ok(response);
		}


		[HttpGet("{id}")]
		public async Task<IActionResult> Get(Guid id)
		{
			var order = await _context.Orders.FindAsync(id);
			if (order == null)
				return NotFound();

			using var client = _httpClientFactory.CreateClient("ProductService");
			var product = await client.GetFromJsonAsync<ProductDto>($"https://localhost:5001/api/products/{order.ProductId}");

			if (product == null)
				return NotFound();

			var OrdeDto = new OrderResponseDto
			{
				Id = order.Id,
				Quantity = order.Quantity,
				TotalPrice = order.TotalPrice,
				Product = product
			};

			return Ok(order);
		}

		[HttpPost]
		public async Task<IActionResult> Create(OrderRequestDto orderPost)
		{
			using var client = _httpClientFactory.CreateClient("ProductService");
			var product = await client.GetFromJsonAsync<ProductDto>($"https://localhost:5001/api/products/{orderPost.ProductId}");

			if (product == null)
				return NotFound("Product not found");

			if (product.Quantity < orderPost.Quantity)
				return BadRequest("insufficient product quantity.");

			var reduceResponse = await client.PutAsync($"https://localhost:5001/api/products/reduce-quantity/{orderPost.ProductId}?quantity={orderPost.Quantity}", null);

			if (!reduceResponse.IsSuccessStatusCode)
				return BadRequest("Failed to reduce product quantity");

			Order order = new Order();
			order.Id = Guid.NewGuid();
			order.TotalPrice = product.Price * orderPost.Quantity;

			_context.Orders.Add(order);
			await _context.SaveChangesAsync();

/*			await _publishEndpoint.Publish(new OrderCreated
			{
				OrderId = order.Id,
				ProductId = order.ProductId,
				Quantity = order.Quantity
			});*/

			return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> UpdateOrder(Guid id, OrderUpdateRequestDto updatedOrder)
		{
			var order = await _context.Orders.FindAsync(id);
			if (order == null) return NotFound("Order not found");

			var pricePerItem = order.TotalPrice/order.Quantity;

			var diffQty = updatedOrder.Quantity - order.Quantity;
			var client = _httpClientFactory.CreateClient("ProductService");
			var response = await client.PutAsync(
				$"https://localhost:5001/api/products/adjust-quantity/{order.ProductId}?difference={diffQty}", null);

			if (!response.IsSuccessStatusCode)
				return BadRequest("Failed to adjust product quantity");

			order.Quantity = updatedOrder.Quantity;
			order.TotalPrice = updatedOrder.Quantity * pricePerItem;

			await _context.SaveChangesAsync();
			return Ok(order);
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteOrder(Guid id)
		{
			var order = await _context.Orders.FindAsync(id);
			if (order == null) return NotFound();

			var client = _httpClientFactory.CreateClient("ProductService");
			var response = await client.PutAsync(
				$"https://localhost:5001/api/products/restore-quantity/{order.ProductId}?quantity={order.Quantity}", null);

			if (!response.IsSuccessStatusCode)
				return BadRequest("Failed to restore product quantity");

			_context.Orders.Remove(order);
			await _context.SaveChangesAsync();

			return NoContent();
		}

	}
}


/*
 [HttpPut("{id}")]
public async Task<IActionResult> UpdateOrder(Guid id, [FromBody] Order updatedOrder)
{
    var existingOrder = await _dbContext.Orders.FindAsync(id);
    if (existingOrder == null)
        return NotFound("Order not found");

    var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == updatedOrder.ProductId);
    if (product == null)
        return BadRequest("Product not found");

    int quantityDiff = updatedOrder.Quantity - existingOrder.Quantity;
    if (product.Quantity < quantityDiff)
        return BadRequest("Not enough stock to increase order quantity.");

    product.Quantity -= quantityDiff;

    existingOrder.ProductId = updatedOrder.ProductId;
    existingOrder.Quantity = updatedOrder.Quantity;
    existingOrder.TotalPrice = product.Price * updatedOrder.Quantity;

    await _dbContext.SaveChangesAsync();

    // Publish OrderUpdated event
    var message = new OrderUpdated
    {
        ProductId = updatedOrder.ProductId,
        QuantityDifference = quantityDiff
    };
    await _publishEndpoint.Publish(message);

    return Ok(existingOrder);
}

 */


/*
 [HttpDelete("{id}")]
public async Task<IActionResult> DeleteOrder(Guid id)
{
    var existingOrder = await _dbContext.Orders.FindAsync(id);
    if (existingOrder == null)
        return NotFound("Order not found");

    var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == existingOrder.ProductId);
    if (product != null)
        product.Quantity += existingOrder.Quantity;

    _dbContext.Orders.Remove(existingOrder);
    await _dbContext.SaveChangesAsync();

    // 🔔 Publish OrderDeleted event
    var message = new OrderDeleted
    {
        ProductId = existingOrder.ProductId,
        Quantity = existingOrder.Quantity
    };
    await _publishEndpoint.Publish(message);

    return NoContent();
}

 */