using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;
        private readonly IUnitOfWork _unitOfWork;
        public UsersController(IUnitOfWork unitOfWork, IMapper mapper, IPhotoService photoService)
        {
            _unitOfWork = unitOfWork;
            _photoService = photoService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<PagedList<MemberDTO>>> GetUsers([FromQuery] UserParams userParams)
        {
            var gender = await _unitOfWork.UserRepository.GetUserGender(User.GetUsername());
            userParams.CurrentUsername = User.GetUsername();
            if (string.IsNullOrEmpty(userParams.Gender)) userParams.Gender = gender == "male" ? "female" : "male";

            var users = await _unitOfWork.UserRepository.GetMembersAsync(userParams);
            Response.AddPaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);

            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MemberDTO>> GetUserById(int id)
        {
            var user = await _unitOfWork.UserRepository.GetUserByIdAsync(id);
            if (user == null) return NotFound("User with this id doesn't exist");
            MemberDTO member = _mapper.Map<MemberDTO>(user);

            return member;
        }

        [HttpGet("me/details")]
        public async Task<ActionResult<MemberDTO>> GetLoggedInUser()
        {
            string username = User.GetUsername();
            MemberDTO member = await _unitOfWork.UserRepository.GetMemberByUsernameAsync(username);
            if (member == null) return NotFound("Not found");
            return member;
        }

        [HttpGet("find/{username}", Name = "GetUserByUsername")]
        public async Task<ActionResult<MemberDTO>> GetUserByUsername(string username)
        {
            MemberDTO user = await _unitOfWork.UserRepository.GetMemberByUsernameAsync(username);
            if (user == null) return NotFound("User with this username doesn't exist");
            return user;
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDTO memberUpdate)
        {
            string username = User.GetUsername();
            AppUser user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(username);
            if (user == null) return NotFound("User not found");

            _mapper.Map(memberUpdate, user);
            _unitOfWork.UserRepository.Update(user);

            if (await _unitOfWork.Save()) return NoContent();

            return BadRequest("Failed to update user");
        }

        [HttpPost("photos")]
        public async Task<ActionResult<PhotoDTO>> AddPhoto(IFormFile file)
        {
            if (file == null) return BadRequest("Upload a file with your request");

            string username = User.GetUsername();
            AppUser user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(username);
            if (user == null) return NotFound("User not found");

            var result = await _photoService.UploadPhotoAsync(file);
            if (result.Error != null) return BadRequest(result.Error.Message);

            Photo photo = new Photo
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };

            if (user.Photos.Count == 0) photo.IsMain = true;
            user.Photos.Add(photo);

            if (await _unitOfWork.Save())
            {
                return CreatedAtRoute("GetUserByUsername", new { username = username }, _mapper.Map<PhotoDTO>(photo));
            }

            return BadRequest("Error adding photo");
        }

        [HttpPut("photos/{photoId}/set_main")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {

            string username = User.GetUsername();
            AppUser user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(username);
            if (user == null) return NotFound("User not found");

            Photo photo = user.Photos.FirstOrDefault(x => x.Id == photoId);
            if (photo == null) return NotFound("Photo not found");

            if (photo.IsMain) return BadRequest("This is already your main photo");

            Photo currentMainPhoto = user.Photos.FirstOrDefault(x => x.IsMain);
            if (currentMainPhoto != null) currentMainPhoto.IsMain = false;

            photo.IsMain = true;

            if (await _unitOfWork.Save()) return NoContent();

            return BadRequest("Error setting main photo");
        }

        [HttpDelete("photos/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId)
        {

            string username = User.GetUsername();
            AppUser user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(username);
            if (user == null) return NotFound("User not found");

            Photo photo = user.Photos.FirstOrDefault(x => x.Id == photoId);
            if (photo == null) return NotFound("Photo not found");

            if (photo.IsMain) return BadRequest("You cannot delete your main photo");

            if (photo.PublicId != null)
            {
                DeletionResult result = await _photoService.DeletePhotoAsync(username);
                if (result.Error != null) return BadRequest(result.Error.Message);
            }

            user.Photos.Remove(photo);

            if (await _unitOfWork.Save()) return Ok();

            return BadRequest("Error deleting photo");
        }
    }
}