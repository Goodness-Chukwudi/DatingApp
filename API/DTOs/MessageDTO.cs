
using System;
using System.Text.Json.Serialization;

namespace API.DTOs
{
    public class MessageDTO
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string SenderUsername { get; set; }
        public string SenderPhotoUrl { get; set; }
        public int ReceiverId { get; set; }
        public string ReceiverUsername { get; set; }
        public string ReceiverPhotoUrl { get; set; }
        public string Content { get; set; }
        public DateTime? DateRead { get; set; }
        public DateTime DateSent { get; set; }
        [JsonIgnore]
        public bool ReceiverDeleted { get; set; }
        [JsonIgnore]
        public bool SenderDeleted { get; set; }
    }
}