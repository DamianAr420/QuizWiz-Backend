namespace QuizWiz_Backend.DTOs
{
    public class UserAdminUpdateDto
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public int Points { get; set; }
        public int Experience { get; set; }
    }

    public class ShopItemCreateDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Price { get; set; }
        public int Type { get; set; }
        public int Rarity { get; set; }
        public string? ImageUrl { get; set; }
        public int? StockQuantity { get; set; }
        public int RequiredLevel { get; set; } = 1;
    }
}