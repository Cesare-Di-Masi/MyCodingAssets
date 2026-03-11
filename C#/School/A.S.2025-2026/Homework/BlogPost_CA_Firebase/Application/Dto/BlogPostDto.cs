namespace Application.Dto
{
    /// <summary>
    /// Data transfer object for blog post representation in the application layer.
    /// Used to transfer blog post data between layers while hiding domain entity details.
    /// </summary>
    public record BlogPostDto
    (
        string Title,
        string Content,
        DateTime? CreatedAt = null,
        string? Id = null
    );
}