using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Models.DTOs;
using OrderService.Services;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<OrderResponseDto>> Create([FromBody] CreateOrderRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var (order, error) = await _orderService.CreateAsync(dto);
            if (order is null)
            {
                if (error == "Product not found")
                {
                    return NotFound(new { message = error });
                }

                return BadRequest(new { message = error ?? "Unable to create order." });
            }

            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }

        [HttpGet("{id:guid}")]
        [Authorize]
        public async Task<ActionResult<OrderResponseDto>> GetById(Guid id)
        {
            var order = await _orderService.GetByIdAsync(id);
            if (order is null)
            {
                return NotFound();
            }

            return Ok(order);
        }

        [HttpGet("user/{userId:guid}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetByUserId(Guid userId)
        {
            var orders = await _orderService.GetByUserIdAsync(userId);
            return Ok(orders);
        }

        [HttpPatch("{id:guid}/status")]
        [Authorize]
        public async Task<ActionResult<OrderResponseDto>> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var updated = await _orderService.UpdateStatusAsync(id, dto);
            if (updated is null)
            {
                return NotFound();
            }

            return Ok(updated);
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public ActionResult<string> Health()
        {
            return Ok("Healthy");
        }
    }
}
