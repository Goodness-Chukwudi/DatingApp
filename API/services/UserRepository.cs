using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
    public class UserRepository : IUserRepository
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        public UserRepository(DataContext context, IMapper mapper)
        {
            _mapper = mapper;
            _context = context;
        }

        public async Task<MemberDTO> GetMemberByUsernameAsync(string username, bool ignoreFilters = false)
        {
            if (ignoreFilters)
            {
                return await _context.Users
                    .Where(x => x.UserName == username)
                    .ProjectTo<MemberDTO>(_mapper.ConfigurationProvider)
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync();
            }

            return await _context.Users
                .Where(x => x.UserName == username)
                .ProjectTo<MemberDTO>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();
        }
        public async Task<PagedList<MemberDTO>> GetMembersAsync(UserParams userParams)
        {
            var userQuery = _context.Users.AsQueryable();
            userQuery = userQuery.Where(u => u.UserName != userParams.CurrentUsername);
            userQuery = userQuery.Where(u => u.Gender == userParams.Gender);

            DateTime minDob = DateTime.Today.AddYears(-userParams.MaxAge - 1);
            DateTime maxDob = DateTime.Today.AddYears(-userParams.MinAge);
            userQuery = userQuery.Where(u => u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob);

            userQuery = userParams.OrderBy switch
            {
                "createdAt" => userQuery.OrderByDescending(u => u.CreatedAt),
                _ => userQuery.OrderByDescending(u => u.LastActive)
            };

            var query = userQuery.ProjectTo<MemberDTO>(_mapper.ConfigurationProvider).AsNoTracking();

            return await PagedList<MemberDTO>.CreateAsync(query, userParams.PageNumber, userParams.PageSize);
        }

        public async Task<PagedList<UserPhotoDTO>> GetPhotosAsync(PhotoParams photoParams)
        {
            var photoQuery = _context.Photos.AsQueryable();

            if (photoParams.Username != null) photoQuery = photoQuery.Where(p => p.appUser.UserName == photoParams.Username);

            if (photoParams.IsApproved != null) photoQuery = photoQuery.Where(p => p.IsApproved == photoParams.IsApproved);

            if (photoParams.IsMain != null) photoQuery = photoQuery.Where(p => p.IsMain == photoParams.IsMain);

            photoQuery = photoParams.OrderBy switch
            {
                "username" => photoQuery.OrderBy(p => p.appUser.UserName),
                _ => photoQuery.OrderByDescending(p => p.CreatedAt)
            };

            var query = photoQuery
                .IgnoreQueryFilters()
                .ProjectTo<UserPhotoDTO>(_mapper.ConfigurationProvider)
                .AsNoTracking();

            return await PagedList<UserPhotoDTO>.CreateAsync(query, photoParams.PageNumber, photoParams.PageSize);
        }

        public async Task<AppUser> GetUserByIdAsync(int id)
        {
            return await _context.Users.Include(x => x.Photos).FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<AppUser> GetUserByUsernameAsync(string username)
        {
            return await _context.Users.Include(x => x.Photos)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.UserName == username);
        }

        public async Task<string> GetUserGender(string username)
        {
            return await _context.Users
                .Where(u => u.UserName == username)
                .Select(u => u.Gender)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<AppUser>> GetUsersAsync()
        {
            return await _context.Users.Include(x => x.Photos).ToListAsync();
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public void Update(AppUser appUser)
        {
            _context.Entry(appUser).State = EntityState.Modified;
        }
    }
}