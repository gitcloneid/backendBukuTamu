using BukuTamuAPI.DTOs;
using BukuTamuAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BukuTamuAPI.Services;


public interface ITamuService
{
    Task<(IEnumerable<TamuResponse> tamu, int totalCount)> GetAllTamu(int page, int limit);
    Task<TamuResponse> CreateTamu(TamuCreateRequest request);
    Task<TamuResponse> UpdateTamu(int id, TamuUpdateRequest request);
    Task<IEnumerable<TamuResponse>> SearchTamu(string query);
}

public class TamuService : ITamuService
{
    private readonly DbtamuContext _context;
    private readonly ILogger<TamuService> _logger;

    public TamuService(DbtamuContext context, ILogger<TamuService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(IEnumerable<TamuResponse>, int)> GetAllTamu(int page = 1, int limit = 10)
    {
        var query = _context.Tamus.AsQueryable();

        var totalCount = await query.CountAsync();

        var tamu = await query
            .OrderBy(t => t.Nama)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(t => new TamuResponse
            {
                IdTamu = t.IdTamu,
                Nama = t.Nama,
                Telepon = t.Telepon
            })
            .ToListAsync();

        return (tamu, totalCount);
    }

    public async Task<TamuResponse> CreateTamu(TamuCreateRequest request)
    {
        var tamu = new Tamu
        {
            Nama = request.Nama,
            Telepon = request.Telepon
        };

        _context.Tamus.Add(tamu);
        await _context.SaveChangesAsync();

        return new TamuResponse
        {
            IdTamu = tamu.IdTamu,
            Nama = tamu.Nama,
            Telepon = tamu.Telepon
        };
    }

    public async Task<TamuResponse> UpdateTamu(int id, TamuUpdateRequest request)
    {
        var tamu = await _context.Tamus.FindAsync(id);
        
        if (tamu == null)
        {
            throw new KeyNotFoundException("Tamu tidak ditemukan");
        }

        if (!string.IsNullOrEmpty(request.Nama))
        {
            tamu.Nama = request.Nama;
        }

        if (!string.IsNullOrEmpty(request.Telepon))
        {
            tamu.Telepon = request.Telepon;
        }

        await _context.SaveChangesAsync();

        return new TamuResponse
        {
            IdTamu = tamu.IdTamu,
            Nama = tamu.Nama,
            Telepon = tamu.Telepon
        };
    }

    public async Task<IEnumerable<TamuResponse>> SearchTamu(string query)
    {
        return await _context.Tamus
            .Where(t => t.Nama.Contains(query) || t.Telepon.Contains(query))
            .Select(t => new TamuResponse
            {
                IdTamu = t.IdTamu,
                Nama = t.Nama,
                Telepon = t.Telepon
            })
            .ToListAsync();
    }
}