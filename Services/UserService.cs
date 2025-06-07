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
        try
        {
            _logger.LogInformation($"[DEBUG] Starting user creation for email: {request.Email}");

            // Check if email already exists
            var emailExists = await _context.Penggunas.AnyAsync(u => u.Email == request.Email);
            if (emailExists)
            {
                _logger.LogWarning($"[DEBUG] Email already exists: {request.Email}");
                throw new ArgumentException("Email sudah digunakan");
            }

            // Validate role
            if (!new[] { "Admin", "Guru", "Penerima Tamu" }.Contains(request.Role))
            {
                _logger.LogWarning($"[DEBUG] Invalid role: {request.Role}");
                throw new ArgumentException("Role tidak valid");
            }

            _logger.LogInformation($"[DEBUG] Creating user object...");

            // Create user object
            var user = new Pengguna
            {
                Nama = request.Nama,
                Email = request.Email,
                Password = BCrypt.HashPassword(request.Password),
                Role = request.Role
            };

            _logger.LogInformation($"[DEBUG] User object created. Adding to context...");

            // Add to context
            _context.Penggunas.Add(user);

            // Check entity state
            var entityState = _context.Entry(user).State;
            _logger.LogInformation($"[DEBUG] Entity state after Add: {entityState}");
            _logger.LogInformation($"[DEBUG] Temporary ID: {user.IdPengguna}");

            // Check pending changes
            var pendingChanges = _context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                           e.State == EntityState.Modified ||
                           e.State == EntityState.Deleted)
                .Count();

            _logger.LogInformation($"[DEBUG] Pending changes count: {pendingChanges}");

            // Log database connection info
            _logger.LogInformation($"[DEBUG] Database provider: {_context.Database.ProviderName}");
            _logger.LogInformation($"[DEBUG] Connection string: {_context.Database.GetConnectionString()}");

            // Test database connection
            try
            {
                await _context.Database.OpenConnectionAsync();
                _logger.LogInformation("[DEBUG] Database connection test: SUCCESS");
                await _context.Database.CloseConnectionAsync();
            }
            catch (Exception connEx)
            {
                _logger.LogError($"[DEBUG] Database connection test: FAILED - {connEx.Message}");
                throw;
            }

            _logger.LogInformation("[DEBUG] Calling SaveChangesAsync...");

            // Save changes with detailed logging
            var saveResult = await _context.SaveChangesAsync();

            _logger.LogInformation($"[DEBUG] SaveChangesAsync returned: {saveResult}");
            _logger.LogInformation($"[DEBUG] Final user ID: {user.IdPengguna}");

            // Verify the user was actually saved by querying back
            _logger.LogInformation($"[DEBUG] Verifying user was saved by re-querying...");

            var savedUser = await _context.Penggunas
                .FirstOrDefaultAsync(u => u.IdPengguna == user.IdPengguna);

            if (savedUser == null)
            {
                _logger.LogError($"[ERROR] User with ID {user.IdPengguna} not found in database after save!");
                throw new InvalidOperationException("User was not saved to database");
            }

            _logger.LogInformation($"[DEBUG] User verification: SUCCESS - Found user {savedUser.Email} with ID {savedUser.IdPengguna}");

            // Also try querying by email to double-check
            var savedUserByEmail = await _context.Penggunas
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (savedUserByEmail == null)
            {
                _logger.LogError($"[ERROR] User with email {request.Email} not found in database after save!");
            }
            else
            {
                _logger.LogInformation($"[DEBUG] Email verification: SUCCESS - Found user with email {savedUserByEmail.Email}");
            }

            var response = new UserResponse
            {
                IdPengguna = user.IdPengguna,
                Nama = user.Nama,
                Email = user.Email,
                Role = user.Role
            };

            _logger.LogInformation($"[DEBUG] Returning response for user ID: {response.IdPengguna}");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ERROR] Exception in CreateUser: {ex.Message}");
            _logger.LogError($"[ERROR] Stack trace: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                _logger.LogError($"[ERROR] Inner exception: {ex.InnerException.Message}");
                _logger.LogError($"[ERROR] Inner stack trace: {ex.InnerException.StackTrace}");
            }

            throw;
        }
    }

    // Add this method for additional debugging
    public async Task<object> GetDatabaseStats()
    {
        try
        {
            var totalUsers = await _context.Penggunas.CountAsync();
            var recentUsers = await _context.Penggunas
                .OrderByDescending(u => u.IdPengguna)
                .Take(5)
                .Select(u => new { u.IdPengguna, u.Email, u.Nama })
                .ToListAsync();

            return new
            {
                TotalUsers = totalUsers,
                RecentUsers = recentUsers,
                DatabaseProvider = _context.Database.ProviderName,
                ConnectionString = _context.Database.GetConnectionString()?.Substring(0, 50) + "...", // Truncated for security
                ContextId = _context.ContextId.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting database stats: {ex.Message}");
            throw;
        }
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
            if (!new[] { "Admin", "Guru", "Penerima Tamu" }.Contains(request.Role))
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