using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class LikesController : BaseApiController
    {
        private readonly IUserRepository _userRepository;
        private readonly ILikesRepository _likesRepository;
        public LikesController(IUserRepository userRepository, ILikesRepository likesRepository)
        {
            _likesRepository = likesRepository;
            _userRepository = userRepository;
        }

        [HttpPost("{username}")]
        public async Task<ActionResult> AddLike(string username)
        {
            int sourceUserId = User.GetUserId();
            AppUser sourceUser = await _likesRepository.GetUserWithLikes(sourceUserId);
            AppUser likedUser = await _userRepository.GetUserByUsernameAsync(username);

            if (likedUser == null) return NotFound();
            if (sourceUser.UserName == username) return BadRequest("You cannot like yourself");

            UserLike like = await _likesRepository.GetUserLike(sourceUserId, likedUser.Id);
            if (like != null) return BadRequest("You have already liked this user");

            like = new UserLike
            {
                SourceUserId = sourceUserId,
                LikedUserId = likedUser.Id
            };
            sourceUser.LikedUsers.Add(like);
            if (await _userRepository.SaveAllAsync()) return Ok();

            return BadRequest("Action failed");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LikeDTO>>> GetUserLikes([FromQuery] LikesParams likesParams)
        {
            likesParams.UserId = User.GetUserId();
            PagedList<LikeDTO> likes = await _likesRepository.GetUserLikes(likesParams);
            Response.AddPaginationHeader(likes.CurrentPage, likes.PageSize, likes.TotalCount, likes.TotalPages);

            return likes;
        }

    }
}