namespace Ordering_Adlawan_IT13.Models
{
    public class FoodItem
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? Category { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
