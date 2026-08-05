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
        try
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        }
        catch (Exception ex) when (ex.Message.Contains("AvatarUrl") || ex.Message.Contains("42703"))
        {
            await _context.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""AvatarUrl"" text;", ct);
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        }
    }

    public async Task<User?> GetByPhoneAsync(string phone, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;

        var raw = phone.Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        var digitsOnly = System.Text.RegularExpressions.Regex.Replace(raw, @"\D", "");
        var digitsNoPrefix = digitsOnly.StartsWith("374") ? digitsOnly.Substring(3) : digitsOnly;

        var option1 = raw;
        var option2 = digitsOnly;
        var option3 = "+" + digitsOnly;
        var option4 = "+374" + digitsNoPrefix;
        var option5 = "374" + digitsNoPrefix;

        try
        {
            return await _context.Users.FirstOrDefaultAsync(
                u => u.Phone == option1 || 
                     u.Phone == option2 || 
                     u.Phone == option3 || 
                     u.Phone == option4 || 
                     u.Phone == option5 ||
                     u.Phone.EndsWith(digitsNoPrefix),
                ct);
        }
        catch (Exception ex) when (ex.Message.Contains("AvatarUrl") || ex.Message.Contains("42703"))
        {
            await _context.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""AvatarUrl"" text;", ct);
            return await _context.Users.FirstOrDefaultAsync(
                u => u.Phone == option1 || 
                     u.Phone == option2 || 
                     u.Phone == option3 || 
                     u.Phone == option4 || 
                     u.Phone == option5 ||
                     u.Phone.EndsWith(digitsNoPrefix),
                ct);
        }
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalizedEmail = email.ToLowerInvariant().Trim();
        try
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);
        }
        catch (Exception ex) when (ex.Message.Contains("AvatarUrl") || ex.Message.Contains("42703"))
        {
            await _context.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""AvatarUrl"" text;", ct);
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);
        }
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
