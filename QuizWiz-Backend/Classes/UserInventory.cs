namespace QuizWiz_Backend.Classes
{
    public class UserInventory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ShopItemId { get; set; }
        public ShopItem? ShopItem { get; set; }
        public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
    }
}