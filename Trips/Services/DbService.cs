using Microsoft.Data.SqlClient;
using Trips.Exeptions;
using Trips.Models;
using Trips.Models.DTOs;

namespace Trips.Services;

// Interfejs definiujący operacje na bazie danych dla wycieczek i klientów
public interface IDbService
{
    // Pobiera listę wszystkich dostępnych wycieczek
    public Task<IEnumerable<TripGetDTO>> GetTripsDetailsAsync();

    // Pobiera szczegóły wycieczek przypisanych do klienta
    public Task<IDictionary<string, TripGetDTO>> GetTripsForClientDetailsAsync(int clientId);
    
    // Tworzy nowego klienta
    public Task<ClientGetDTO> CreateClientAsync(ClientCreateDTO clientCreateDTO);
    
    // Przypisuje klientowi wycieczkę
    public Task<ClientTrip> PutClientTripAsync(int clientId, int tripId);
    
    // Usuwa przypisanie klienta do wycieczki
    public Task<string> DeleteClientTripAsync(int clientId, int tripId);
}

// Implementacja interfejsu IDbService, obsługująca operacje na bazie danych
public class DbService(IConfiguration config) : IDbService
{
    // Pobiera domyślny łańcuch połączenia z konfiguracji aplikacji
    private readonly string? _connectionString = config.GetConnectionString("Default");

    // Pobiera listę wszystkich dostępnych wycieczek z bazy danych
    public async Task<IEnumerable<TripGetDTO>> GetTripsDetailsAsync()
    {
        var result = new List<TripGetDTO>();
        
        await using var connection = new SqlConnection(_connectionString);
        const string sql = "SELECT idTrip, Name, Description, DateFrom, DateTo, MaxPeople FROM Trip";
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new TripGetDTO()
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                DateFrom = reader.GetDateTime(3),
                DateTo = reader.GetDateTime(4),
                MaxPeople = reader.GetInt32(5),
            });
        }

        return result;
    }

    // Pobiera szczegóły wycieczek przypisanych do klienta na podstawie jego ID
    public async Task<IDictionary<string, TripGetDTO>> GetTripsForClientDetailsAsync(int id)
    {
        var clientTrip = new ClientTripGetDTO();
        await using var connection = new SqlConnection(_connectionString);
        const string sql = "SELECT RegisteredAt, PaymentDate FROM Client_Trip WHERE idClient = @idClient";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@idClient", id);
        await connection.OpenAsync();

        await using (var reader = await command.ExecuteReaderAsync())
        {
            if (!reader.HasRows)
            {
                throw new NotFoundException($"Client with id: {id} does not exist or has no associated trips");
            }

            while (await reader.ReadAsync())
            {
                clientTrip.RegisteredAt = reader.GetInt32(0);
                clientTrip.PaymentDate = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            }
        }

        var key = clientTrip.ToString();
        var result = new Dictionary<string, TripGetDTO>();

        // Pobiera wycieczki przypisane do danego klienta
        const string sql1 = "SELECT Trip.idTrip, Name, Description, DateFrom, DateTo, MaxPeople FROM Trip LEFT JOIN Client_Trip ON Trip.IdTrip = Client_Trip.IdTrip WHERE IdClient = @id";
        await using var command1 = new SqlCommand(sql1, connection);
        command1.Parameters.AddWithValue("@id", id);
        await using var reader1 = await command1.ExecuteReaderAsync();

        while (await reader1.ReadAsync())
        {
            result[key] = new TripGetDTO()
            {
                Id = reader1.GetInt32(0),
                Name = reader1.GetString(1),
                Description = reader1.GetString(2),
                DateFrom = reader1.GetDateTime(3),
                DateTo = reader1.GetDateTime(4),
                MaxPeople = reader1.GetInt32(5),
            };
        }

        return result;
    }

    // Tworzy nowego klienta w bazie danych
    public async Task<ClientGetDTO> CreateClientAsync(ClientCreateDTO client)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = "INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel) VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel); SELECT SCOPE_IDENTITY()";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@FirstName", client.FirstName);
        command.Parameters.AddWithValue("@LastName", client.LastName);
        command.Parameters.AddWithValue("@Email", client.Email);
        command.Parameters.AddWithValue("@Telephone", client.PhoneNumber);
        command.Parameters.AddWithValue("@Pesel", client.Pesel);
        await connection.OpenAsync();
        var id = Convert.ToInt32(await command.ExecuteScalarAsync());

        return new ClientGetDTO()
        {
            Id = id,
            FirstName = client.FirstName,
            LastName = client.LastName,
            Email = client.Email,
            PhoneNumber = client.PhoneNumber,
            Pesel = client.Pesel,
        };
    }

    // Przypisuje klienta do wycieczki
    public async Task<ClientTrip> PutClientTripAsync(int clientId, int tripId)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = "SELECT 1 FROM Client_Trip WHERE idClient = @idClient AND IdTrip = @IdTrip";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@idClient", clientId);
        command.Parameters.AddWithValue("@IdTrip", tripId);
        await connection.OpenAsync();

        await using (var reader = await command.ExecuteReaderAsync())
        {
            if (reader.HasRows)
            {
                throw new AlreadyExistsExeption($"Trip with id: {tripId} for client {clientId} already exists");
            }
        }

        // Sprawdzenie, czy liczba uczestników wycieczki nie przekracza limitu
        const string sql1 = "SELECT 1 FROM Trip WHERE IdTrip = @idTrip AND MaxPeople <= (SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @idTrip)";
        await using var command1 = new SqlCommand(sql1, connection);
        command1.Parameters.AddWithValue("@idTrip", tripId);
        await using var reader1 = await command1.ExecuteReaderAsync();

        if (reader1.HasRows)
        {
            throw new OutOfLimitExeption($"Trip with id: {tripId} has reached max capacity");
        }

        int currentDate = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
        // Przypisanie klienta do wycieczki
        const string sql2 = "INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt) VALUES (@clientId, @tripId, @RegisteredAt)";
        await using var command2 = new SqlCommand(sql2, connection);
        command2.Parameters.AddWithValue("@clientId", clientId);
        command2.Parameters.AddWithValue("@tripId", tripId);
        command2.Parameters.AddWithValue("@RegisteredAt", currentDate);

        return new ClientTrip()
        {
            IdClient = clientId,
            IdTrip = tripId,
            RegisteredAt = currentDate,
            PaymentDate = 0
        };
    }
    // Metoda do usunięcia przypisania klienta do wycieczki
    public async Task<string> DeleteClientTripAsync(int clientId, int tripId)
    {
        await using var connection = new SqlConnection(_connectionString);

        // Sprawdzenie, czy klient jest przypisany do wycieczki
        const string sql = "SELECT 1 FROM Client_Trip WHERE idClient = @idClient AND IdTrip = @IdTrip";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@idClient", clientId);
        command.Parameters.AddWithValue("@IdTrip", tripId);
        await connection.OpenAsync();

        await using (var reader = await command.ExecuteReaderAsync())
        {
            if (!reader.HasRows)
            {
                // Jeśli przypisanie nie istnieje, zwrócenie wyjątku `NotFoundException`
                throw new NotFoundException($"Trip with id: {tripId} for client {clientId} does not exist");
            }
        }

        // Usunięcie przypisania klienta do wycieczki
        const string sql1 = "DELETE FROM Client_Trip WHERE idClient = @idClient AND IdTrip = @IdTrip";
        await using var command1 = new SqlCommand(sql1, connection);
        command1.Parameters.AddWithValue("@idClient", clientId);
        command1.Parameters.AddWithValue("@IdTrip", tripId);

        // Wykonanie zapytania DELETE
        await command1.ExecuteNonQueryAsync();

        return $"Trip with id: {tripId} for client: {clientId} deleted successfully";
    }

}
