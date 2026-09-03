using System.Collections.Generic;

namespace _8F.Models
{
    public class Response
    {
        public int FC;
        public int CN;
        public int OR;
        public bool IsBalacenced = false;
        public List<FreqResult> FD = default!;
        public int ERR { get; set; }
    }
}
