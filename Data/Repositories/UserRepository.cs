using Microsoft.EntityFrameworkCore;
using movaa_project_back.Data;
using movaa_project_back.Domain.Entities;
using movaa_project_back.Domain.Interfaces;

namespace movaa_project_back.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<User?> GetByPhoneAsync(string phone, CancellationToken ct = default)
    {
        var raw = phone.Trim().Replace(" ", "");
        var digits = raw.StartsWith("+374") ? raw.Substring(4) : (raw.StartsWith("374") ? raw.Substring(3) : raw);
        var formattedWithPlus = "+374" + digits;
        var formattedClean = "374" + digits;

        return await _context.Users.FirstOrDefaultAsync(
            u => u.Phone == raw || u.Phone == digits || u.Phone == formattedWithPlus || u.Phone == formattedClean,
            ct);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalizedEmail = email.ToLowerInvariant().Trim();
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);
    }

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        await _context.Users.AddAsync(user, ct);
        await _context.SaveChangesAsync(ct);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(ct);
    }
}
