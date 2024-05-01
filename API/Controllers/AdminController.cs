
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Authorize(Policy = "AdminRole")]
    public class AdminController : BaseApiController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        public AdminController(UserManager<AppUser> userManager, IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        [HttpGet("users-with-roles")]
        public async Task<ActionResult> GetUsersWithRoles()
        {
            var users = await _userManager.Users
                .Include(ur => ur.UserRoles)
                .ThenInclude(r => r.Role)
                .OrderBy(u => u.UserName)
                .Select(u => new
                {
                    u.Id,
                    username = u.UserName,
                    Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPatch("edit-roles/{username}")]
        public async Task<ActionResult> EditUserRoles(string username, [FromQuery] string roles)
        {
            var selectedRoles = roles.Split(",").ToArray();
            var user = await _userManager.FindByNameAsync(username);
            var userRoles = await _userManager.GetRolesAsync(user);
            var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));
            if (!result.Succeeded) return BadRequest("Failed to add roles");
            result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

            return Ok(_userManager.GetRolesAsync(user));
        }

        [HttpPatch("users/{username}/photos/{photoId}/approve")]
        public async Task<ActionResult<MemberDTO>> GetUserById(string username, int photoId)
        {
            var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(username);
            if (user == null) return NotFound("User not found");

            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);
            if (photo == null) return NotFound("Photo not found");

            if (photo.IsApproved) return BadRequest("This photo is already approved");

            photo.IsApproved = true;

            Photo currentMainPhoto = user.Photos.FirstOrDefault(x => x.IsMain);
            if (currentMainPhoto == null) photo.IsMain = true;

            if (await _unitOfWork.Save()) return NoContent();

            return BadRequest("Error approving photo");
        }

        [HttpGet("users/photos")]
        public async Task<ActionResult<PagedList<UserPhotoDTO>>> GetUsersPhotos([FromQuery] PhotoParams photoParams)
        {
            var photos = await _unitOfWork.UserRepository.GetPhotosAsync(photoParams);
            Response.AddPaginationHeader(photos.CurrentPage, photos.PageSize, photos.TotalCount, photos.TotalPages);

            return Ok(photos);
        }
    }
}