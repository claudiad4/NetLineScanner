namespace NetLine.Application.DTO;

public record OfficeDto(
    int Id,
    string Name,
    string? Location,
    DateTime CreatedAt);
