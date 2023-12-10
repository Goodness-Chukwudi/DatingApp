using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTOs
{
    public class MemberUpdateDTO
    {
        public string NickName { get; set; }
        public string About { get; set; }
        public string InterestedIn { get; set; }
        public string Hobbies { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }
}