namespace Infrastructure.Dto
{
    /// <summary>
    /// Data transfer object for blog post representation in the persistence layer.
    /// Stores blog post data in a format suitable for file-based storage.
    /// Uses Unix timestamp for date serialization.
    /// </summary>
    public record BlogPostPersistenceDto
    (
        string Title,
        string Content,
        long timestamp,
        string Id
    );
}