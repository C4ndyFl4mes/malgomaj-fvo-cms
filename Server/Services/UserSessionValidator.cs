using Microsoft.EntityFrameworkCore;
using Server.API.Data;
using Server.API.Entities;

namespace Server.Services;

// This class is meant to validate a session.
public class UserSessionValidator(AppDbContext ctx)
{
    // For the moment, it only checks if the user still exists.
    public async Task<bool> IsSessionValidAsync(string userId, CancellationToken ct)
    {
        if (!Guid.TryParse(userId, out Guid id))
            return false;
        
        UserEntity? user = await ctx.Users.FirstOrDefaultAsync(user => user.Id == id, ct);

        if (user is null)
            return false;
        
        return true;
    }
}