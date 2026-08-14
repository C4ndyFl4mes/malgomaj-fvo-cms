using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.API.Data;
using Server.API.Entities;
using Server.API.Exceptions;
using Server.Services;

namespace Server.API.Routes.Internal.Authentication.SignIn;

public class SignInData(IConfiguration config, AppDbContext ctx)
{
    public async Task<Token> SignIn(UserModel model, CancellationToken ct)
    {
        UserEntity user = await ctx.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == model.Email, ct) ??
            throw new BadRequestException("Ogiltiga inloggningsuppgifter.");
        
        if (new PasswordHasher<UserEntity>().VerifyHashedPassword(user, user.PasswordHash, model.Password) == PasswordVerificationResult.Failed)
            throw new BadRequestException("Ogiltiga inloggningsuppgifter.");
        
        TokenService tokenService = new(config, ctx);
        return new()
        {
            AccessToken = tokenService.CreateToken(user),
            RefreshToken = await tokenService.GenerateAndSaveRefreshTokenAsync(user, ct)
        };
    }
}