using Microsoft.AspNetCore.Mvc;
using Trips.Exeptions;
using Trips.Models;
using Trips.Models.DTOs;
using Trips.Services;

namespace Trips.Controllers;

// Deklaracja kontrolera API dla klientów
[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    // Endpoint do pobierania informacji o wycieczkach danego klienta
    [HttpGet("{id}/trips")]
    public async Task<IActionResult> GetTripsForClientAsync(IDbService dbService, int id)
    {
        try
        {
            // Pobiera szczegóły wycieczek klienta na podstawie jego ID
            return Ok(await dbService.GetTripsForClientDetailsAsync(id));
        }
        catch (NotFoundException e)
        {
            // Zwraca błąd 404, jeśli klient nie został znaleziony
            return NotFound(e.Message);
        }
    }

    // Endpoint do tworzenia nowego klienta
    [HttpPost]
    public async Task<IActionResult> PostTripsForClientAsync(IDbService dbService, [FromBody] ClientCreateDTO client)
    {
        // Tworzy nowego klienta na podstawie dostarczonych danych
        return Ok(await dbService.CreateClientAsync(client));
    }

    // Endpoint do przypisywania klientowi wycieczki
    [HttpPut("{clientId}/trips/{tripId}")]
    public async Task<IActionResult> PutTripsForClientAsync(IDbService dbService, int clientId, int tripId)
    {
        try
        {
            // Przypisuje klientowi wycieczkę na podstawie ich ID
            return Ok(await dbService.PutClientTripAsync(clientId, tripId));
        }
        catch (AlreadyExistsExeption e)
        {
            // Zwraca błąd 409, jeśli klient już jest przypisany do tej wycieczki
            return Conflict(e.Message);
        }
        catch (OutOfLimitExeption e)
        {
            // Zwraca błąd 409, jeśli przypisanie przekracza dozwolone limity
            return Conflict(e.Message);
        }
    }

    // Endpoint do usuwania przypisania wycieczki do klienta
    [HttpDelete("{clientId}/trips/{tripId}")]
    public async Task<IActionResult> DeleteTripsForClientAsync(IDbService dbService, int clientId, int tripId)
    {
        try
        {
            // Usuwa przypisanie wycieczki dla danego klienta
            return Ok(await dbService.DeleteClientTripAsync(clientId, tripId));
        }
        catch (NotFoundException e)
        {
            // Zwraca błąd 404, jeśli przypisanie nie istnieje
            return NotFound(e.Message);
        }
    }
}
