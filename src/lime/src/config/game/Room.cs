namespace lime.config.data.game
{
    using global::System.Collections.Generic;
    
    public class Room
    {
        public int cost { get; set; }

        public List<DropItem> drop { get; set; }
    }
}
