namespace WebApit4s.Models
{
    public class ConsentQuestionResponse
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public string Answer { get; set; } // Yes / No
    }
}
