namespace Infrastructure.Dto
{
    public record MeasurementJsonDto
    (
         string SensorId,
         double Value,
         DateTime Timestamp,
         string Type
    );
}