namespace CentralPress.API.Documents;

/// <summary>
/// Thrown for data validation issues that block document printing; the controller maps this to 422 Unprocessable Entity.
/// </summary>
public sealed class PrintDataException(string message) : Exception(message);
