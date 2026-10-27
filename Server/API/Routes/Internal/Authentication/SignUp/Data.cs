using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.API.Data;
using Server.API.Entities;
using Server.API.Exceptions;

namespace Server.API.Routes.Internal.Authentication.SignUp;

public class SignUpData(AppDbContext ctx, IAuthorizationService authorization)
{
    public async Task<AuthenticationResponse> SignUp(ClaimsPrincipal user, UserModel model, CancellationToken ct)
    {
        AuthorizationResult result = await authorization.AuthorizeAsync(user, "AdministratorsOnly");
        if (!result.Succeeded)
            throw new UnauthorizedException("Du har inte tillgång till denna operation.");

        if (await ctx.Users.AnyAsync(u => u.Email == model.Email, ct))
            throw new BadRequestException("En användare med den E-postadressen finns redan.");
        
        RoleEntity role = await ctx.Roles.FirstOrDefaultAsync(r => r.Name == "Editor", ct) ??
            throw new InvalidOperationException("Standardrollen 'Editor' är inte konfigurerad i databasen.");
        
        UserEntity newUser = new()
        {
            Id = Guid.NewGuid(),
            Email = model.Email,
            PasswordHash = new PasswordHasher<UserEntity>().HashPassword(null!, model.Password),
            RoleId = role.Id,
            Role = null! // Will be set by EF Core.
        };

        ctx.Users.Add(newUser);
        await ctx.SaveChangesAsync(ct);

        return new AuthenticationResponse
        {
            Message = "Användaren har skapats"
        };
    }
}