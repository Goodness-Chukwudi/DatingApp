using System;
using System.Collections.Generic;

namespace API.DTOs
{
    public class MemberDTO
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public int Age { get; set; }
        public string NickName { get; set; }
        public string Gender { get; set; }
        public string About { get; set; }
        public string InterestedIn { get; set; }
        public string Hobbies { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActive { get; set; }
        public ICollection<PhotoDTO> Photos { get; set; }
    }
}