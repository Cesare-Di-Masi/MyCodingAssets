namespace Application.Dto
{
    public record BlogPostDto
    (
        string Title,
        string Content,
        DateTime? CreatedAt = null,
        Guid? ID = null
        );
}
