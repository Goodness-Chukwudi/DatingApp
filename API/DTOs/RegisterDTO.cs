
using System;
using System.ComponentModel.DataAnnotations;

namespace API.DTOs
{
    public class RegisterDTO
    {
        [Required(ErrorMessage = "Username is required")] public string Username { get; set; }
        [Required(ErrorMessage = "Password is required")] public string Password { get; set; }
        [Required(ErrorMessage = "Date of birth is required")] public DateTime DateOfBirth { get; set; }
        [Required(ErrorMessage = "Nick name is required")] public string NickName { get; set; }
        [Required(ErrorMessage = "Gender is required")] public string Gender { get; set; }
        [Required(ErrorMessage = "About is required")] public string About { get; set; }
        public string InterestedIn { get; set; }
        public string Hobbies { get; set; }
        [Required(ErrorMessage = "City is required")] public string City { get; set; }
        [Required(ErrorMessage = "Country is required")] public string Country { get; set; }

    }
}