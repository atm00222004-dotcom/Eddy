using System.Collections.Generic;

namespace _8F.Models
{
    public class CordinateQueue
    {
        public List<Cordinate> cordinates { get; set; } = default!;
        public bool IsRelevant { get; set; }
        public int Action { get; set; }
    }
}
