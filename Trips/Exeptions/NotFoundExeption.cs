namespace Trips.Exeptions;

public class NotFoundException(string message) : Exception(message);