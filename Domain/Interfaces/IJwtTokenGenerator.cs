using movaa_project_back.Domain.Entities;

namespace movaa_project_back.Domain.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
    string GenerateAdminToken(Admin admin);
}
