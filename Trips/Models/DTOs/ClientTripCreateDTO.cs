namespace Trips.Models.DTOs;

public class ClientTripCreateDTO
{
    public required int IdClient { get; set; }
    public required int IdTrip { get; set; }
    public required int RegisteredAt { get; set; }
    public required int PaymentDate { get; set; }
}