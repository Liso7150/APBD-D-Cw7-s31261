using Microsoft.AspNetCore.Mvc;
using Trips.Services;

namespace Trips.Controllers;

// Deklaracja kontrolera API dla wycieczek
[ApiController]
[Route("api/[controller]")]
public class TripsController() : ControllerBase
{
    // Endpoint do pobierania listy wszystkich wycieczek
    [HttpGet]
    public async Task<IActionResult> GetTripsAsync(IDbService dbService)
    {
        // Pobiera szczegóły wszystkich dostępnych wycieczek
        return Ok(await dbService.GetTripsDetailsAsync());
    }
}