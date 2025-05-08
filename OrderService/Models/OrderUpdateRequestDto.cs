namespace OrderService.Models
{
	public class OrderUpdateRequestDto
	{
		public Guid Id { get; set; }
		public int Quantity { get; set; }
	}
}
