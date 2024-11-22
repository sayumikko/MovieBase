﻿namespace MovieBase.Models
{
    public class Tag
    {
        public int TagId { get; set; }
        public string Name { get; set; }

        public ICollection<MovieTag> MovieTags { get; set; } = new List<MovieTag>();
    }
}
