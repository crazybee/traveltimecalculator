using System.Threading.Tasks;
using TraveltimeCalculator.Models;

namespace TraveltimeCalculator.Services
{
    public interface IMessageSender
    {
        Task<bool> SendTravelTimeRequestAsync(TravelTimeRequest request);
    }
}
