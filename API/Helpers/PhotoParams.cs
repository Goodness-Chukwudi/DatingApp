using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Helpers
{
    public class PhotoParams : PaginationParams
    {
        public string Username { get; set; }
        public bool? IsMain { get; set; }
        public bool? IsApproved { get; set; }
        public string OrderBy { get; set; } = "createdAt";

    }
}