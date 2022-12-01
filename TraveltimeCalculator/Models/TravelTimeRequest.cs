using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TraveltimeCalculator.Models
{
    public class TravelTimeRequest
    {
        public string Email { get; set; }

        public bool FromHomeToWork { get; set; } = true;


    }
}
