using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
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

        public async Task<IEnumerable<LikeDTO>> GetUserLikes(string predicate, int userId)
        {
            IQueryable<AppUser> userQuery = _context.Users.OrderBy(u => u.UserName).AsQueryable();
            IQueryable<UserLike> likeQuery = _context.Likes.AsQueryable();

            if (predicate == "liked")
            {
                likeQuery = likeQuery.Where(l => l.SourceUserId == userId);
                userQuery = likeQuery.Select(l => l.LikedUser);
            }

            if (predicate == "likedBy")
            {
                likeQuery = likeQuery.Where(l => l.LikedUserId == userId);
                userQuery = likeQuery.Select(l => l.SourceUser);
            }

            List<LikeDTO> likes = await userQuery.Select(u => new LikeDTO
            {
                Id = u.Id,
                Username = u.UserName,
                Age = u.DateOfBirth.CalculateAge(),
                NickName = u.NickName,
                PhotoUrl = u.Photos.FirstOrDefault(p => p.IsMain).Url,
                City = u.City
            }).ToListAsync();

            return likes;
        }

        public async Task<AppUser> GetUserWithLikes(int userId)
        {
            return await _context.Users
                .Include(u => u.LikedUsers)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
    }
}