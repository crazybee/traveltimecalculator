using Microsoft.AspNetCore.Mvc.Rendering;

namespace TraveltimeCalculator.Models
{
    public class IndexModel
    {
        public string Email { get; set; } = "zheliu@outlook.com";
        public bool IsSuccessful { get; set; } = false;

        public bool IsFromHome { get; set; } = true;

    }
}
