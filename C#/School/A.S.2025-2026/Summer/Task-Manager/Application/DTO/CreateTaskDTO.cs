namespace Application.DTO
{
    public class CreateTaskDTO
    {
        public string Name { get; set; } = string.Empty;
        public double EstimatedTimeSeconds { get; set; }
        public Guid? GroupId { get; set; }
    }
}