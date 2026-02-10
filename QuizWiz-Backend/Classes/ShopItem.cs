namespace QuizWiz_Backend.Classes
{
    public enum ItemRarity { Common, Rare, Epic, Legendary }
    public enum ItemType { AvatarFrame, Background, Badge, Ticket }

    public class ShopItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Price { get; set; }
        public ItemType Type { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ItemRarity Rarity { get; set; }
        public int? StockQuantity { get; set; }
        public bool IsAvailable => !StockQuantity.HasValue || StockQuantity > 0;
        public int RequiredLevel { get; set; } = 1;
    }
}