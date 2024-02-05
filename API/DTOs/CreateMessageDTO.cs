using System.ComponentModel.DataAnnotations;

namespace API.DTOs
{
    public class CreateMessageDTO
    {
        [Required(ErrorMessage = "Receiver username is required")]
        public string ReceiverUsername { get; set; }
        [Required(ErrorMessage = "Message is required")]
        public string Content { get; set; }
    }
}