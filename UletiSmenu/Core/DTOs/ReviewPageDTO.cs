namespace Core.DTOs
{
    public class ReviewPageDTO
    {
        public Guid SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public ReviewSummaryDTO Summary { get; set; } = new();
        public List<ReviewDTO> Reviews { get; set; } = new();
    }
}
