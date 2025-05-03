namespace Trips.Models.DTOs;

public class ClientTripGetDTO
{
    public int RegisteredAt { get; set; }
    public int PaymentDate { get; set; }

    public override string ToString()
    {
        var key = $"RegisteredAt: {RegisteredAt}";
        if (PaymentDate != 0) { key += $"\n PaymentDate: {PaymentDate}"; }
        return key;
    }
}