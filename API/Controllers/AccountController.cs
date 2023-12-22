using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        public AccountController(DataContext context, ITokenService tokenService, IMapper mapper)
        {
            _mapper = mapper;
            _tokenService = tokenService;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDTO>> RegisterUser(RegisterDTO registrationData)
        {
            Console.WriteLine("HEre");
            if (await UserExist(registrationData.Username)) return BadRequest("A user with this username already exist");
            using var hmac = new HMACSHA512();
            AppUser appUser = _mapper.Map<AppUser>(registrationData);

            appUser.UserName = registrationData.Username.ToLower();
            appUser.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registrationData.Password));
            appUser.PasswordSalt = hmac.Key;

            _context.Users.Add(appUser);
            await _context.SaveChangesAsync();

            return new UserDTO
            {
                Username = appUser.UserName,
                NickName = appUser.NickName,
                About = appUser.About,
                PhotoUrl = appUser.Photos?.FirstOrDefault(x => x.IsMain)?.Url
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDTO>> Login(LoginDTO loginData)
        {
            AppUser user = await _context.Users.SingleOrDefaultAsync(user => user.UserName == loginData.Username);
            if (user == null) return BadRequest("Invalid username or password");

            using HMACSHA512 hmac = new HMACSHA512(user.PasswordSalt);
            byte[] computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginData.Password));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid password or email");
            }

            return new UserDTO
            {
                Username = user.UserName,
                NickName = user.NickName,
                About = user.About,
                PhotoUrl = user.Photos?.FirstOrDefault(x => x.IsMain)?.Url,
                Token = _tokenService.CreateToken(user)
            };

        }

        private async Task<bool> UserExist(string username)
        {
            return await _context.Users.AnyAsync(user => user.UserName == username.ToLower());
        }
    }
}