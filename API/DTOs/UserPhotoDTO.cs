using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTOs
{
    public class UserPhotoDTO
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public bool IsMain { get; set; }
        public bool IsApproved { get; set; } = false;
        public string PublicId { get; set; }
        public string Username { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}