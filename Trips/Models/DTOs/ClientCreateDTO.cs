using System.ComponentModel.DataAnnotations;

namespace Trips.Models.DTOs;

public class ClientCreateDTO
{
    public required int Id { get; set; }
    [Length(1,120)]
    public required string FirstName { get; set; }
    [Length(1,120)]
    public required string LastName { get; set; }
    [EmailAddress]
    public required string Email { get; set; }
    [Phone]
    public required string PhoneNumber { get; set; }
    [Length(11,11)]
    public required string Pesel { get; set; }
}