namespace BukuTamuAPI.Services; 

using BCrypt.Net;
using BukuTamuAPI.DTOs;
using BukuTamuAPI.Models;
using Microsoft.EntityFrameworkCore;

public interface IUserService
{
    Task<(IEnumerable<UserResponse> users, int totalCount)> GetAllUsers(string? role, int page, int limit);
    Task<UserResponse> GetUserById(int id);
    Task<UserResponse> CreateUser(UserCreateRequest request);
    Task<UserResponse> UpdateUser(int id, UserUpdateRequest request);
    Task DeleteUser(int id);
}

public class UserService : IUserService
{
    private readonly DbtamuContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(DbtamuContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(IEnumerable<UserResponse>, int)> GetAllUsers(string? role, int page = 1, int limit = 10)
    {
        var query = _context.Penggunas.AsQueryable();

        if (!string.IsNullOrEmpty(role))
        {
            query = query.Where(u => u.Role == role);
        }

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.Nama)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(u => new UserResponse
            {
                IdPengguna = u.IdPengguna,
                Nama = u.Nama,
                Email = u.Email,
                Role = u.Role
            })
            .ToListAsync();

        return (users, totalCount);
    }

    public async Task<UserResponse> GetUserById(int id)
    {
        var user = await _context.Penggunas.FindAsync(id);
        
        if (user == null)
        {
            throw new KeyNotFoundException("User tidak ditemukan");
        }

        return new UserResponse
        {
            IdPengguna = user.IdPengguna,
            Nama = user.Nama,
            Email = user.Email,
            Role = user.Role
        };
    }

    public async Task<UserResponse> CreateUser(UserCreateRequest request)
    {
        if (await _context.Penggunas.AnyAsync(u => u.Email == request.Email))
        {
            throw new ArgumentException("Email sudah digunakan");
        }

        if (!new[] { "Admin", "Guru", "PenerimaTamu" }.Contains(request.Role))
        {
            throw new ArgumentException("Role tidak valid");
        }

        var user = new Pengguna
        {
            Nama = request.Nama,
            Email = request.Email,
            Password = BCrypt.HashPassword(request.Password),
            Role = request.Role
        };

        _context.Penggunas.Add(user);
        await _context.SaveChangesAsync();

        return new UserResponse
        {
            IdPengguna = user.IdPengguna,
            Nama = user.Nama,
            Email = user.Email,
            Role = user.Role
        };
    }

    public async Task<UserResponse> UpdateUser(int id, UserUpdateRequest request)
    {
        var user = await _context.Penggunas.FindAsync(id);
        
        if (user == null)
        {
            throw new KeyNotFoundException("User tidak ditemukan");
        }

        if (!string.IsNullOrEmpty(request.Email) && 
            request.Email != user.Email && 
            await _context.Penggunas.AnyAsync(u => u.Email == request.Email))
        {
            throw new ArgumentException("Email sudah digunakan");
        }

        if (!string.IsNullOrEmpty(request.Nama))
        {
            user.Nama = request.Nama;
        }

        if (!string.IsNullOrEmpty(request.Email))
        {
            user.Email = request.Email;
        }

        if (!string.IsNullOrEmpty(request.Role))
        {
            if (!new[] { "Admin", "Guru", "PenerimaTamu" }.Contains(request.Role))
            {
                throw new ArgumentException("Role tidak valid");
            }
            user.Role = request.Role;
        }

        await _context.SaveChangesAsync();

        return new UserResponse
        {
            IdPengguna = user.IdPengguna,
            Nama = user.Nama,
            Email = user.Email,
            Role = user.Role
        };
    }

    public async Task DeleteUser(int id)
    {
        var user = await _context.Penggunas.FindAsync(id);
        
        if (user == null)
        {
            throw new KeyNotFoundException("User tidak ditemukan");
        }

        var hasAppointments = await _context.JanjiTemus.AnyAsync(j => j.IdGuru == id);
        if (hasAppointments)
        {
            throw new InvalidOperationException("User tidak dapat dihapus karena memiliki janji temu");
        }

        _context.Penggunas.Remove(user);
        await _context.SaveChangesAsync();
    }
}