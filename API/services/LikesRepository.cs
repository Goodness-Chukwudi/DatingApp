using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
    public class LikesRepository : ILikesRepository
    {
        private readonly DataContext _context;
        public LikesRepository(DataContext context)
        {
            _context = context;
        }

        public async Task<UserLike> GetUserLike(int sourceUserId, int likedUserId)
        {
            return await _context.Likes.FindAsync(sourceUserId, likedUserId);
        }

        public async Task<PagedList<LikeDTO>> GetUserLikes(LikesParams likesParams)
        {
            IQueryable<AppUser> userQuery = _context.Users.OrderBy(u => u.UserName).AsQueryable();
            IQueryable<UserLike> likeQuery = _context.Likes.AsQueryable();

            if (likesParams.Predicate == "liked")
            {
                likeQuery = likeQuery.Where(l => l.SourceUserId == likesParams.UserId);
                userQuery = likeQuery.Select(l => l.LikedUser);
            }

            if (likesParams.Predicate == "likedBy")
            {
                likeQuery = likeQuery.Where(l => l.LikedUserId == likesParams.UserId);
                userQuery = likeQuery.Select(l => l.SourceUser);
            }

            IQueryable<LikeDTO> likes = userQuery.Select(u => new LikeDTO
            {
                Id = u.Id,
                Username = u.UserName,
                Age = u.DateOfBirth.CalculateAge(),
                NickName = u.NickName,
                PhotoUrl = u.Photos.FirstOrDefault(p => p.IsMain).Url,
                City = u.City
            });

            return await PagedList<LikeDTO>.CreateAsync(likes, likesParams.PageNumber, likesParams.PageSize);
        }

        public async Task<AppUser> GetUserWithLikes(int userId)
        {
            return await _context.Users
                .Include(u => u.LikedUsers)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
    }
}