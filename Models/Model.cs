
using System.ComponentModel.DataAnnotations;

namespace UpdatedAttendanceTask.Models
{
    public class Model
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        [Required]
        public string Prompt { get; set; }
        public string Response { get; set; }
        public IFormFile File { get; set; }
    }
}