using System;
using System.Collections.Generic;

namespace SEE.Net
{
    public class ServerSnapshot
    {
        public DateTime Timestamp { get; set; }

        public IEnumerable<SeeCitySnapshot> CitySnapshots { get; set; }
    }
}
