namespace lime.config.data.game
{
    using global::System.Collections.Generic;

    public class Config
    {
        public class ShopItem
        {
            public string id { get; set; }
        }
        public List<ShopItem> shopItems { get; set; }

        public class Room
        {
            public string configPath { get; set; }
        }
        public List<Room> rooms { get; set; }
    }

    public class Game
    {
        [Path("Config.asset", true)]
        public Config Config { get; set; }
        
        [Path("Rooms")]
        public Dictionary<string, Room> Rooms { get; set; }
    }
}
