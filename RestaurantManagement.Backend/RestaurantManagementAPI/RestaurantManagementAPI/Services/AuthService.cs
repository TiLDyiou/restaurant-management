using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestaurentManagementAPI.Data;
using RestaurentManagementAPI.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using RestaurentManagementAPI.Services;
using RestaurentManagementAPI.DTOs;
namespace RestaurantManagementAPI.Services;

public class AuthService : IAuthService
{
    private readonly QLNHDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(QLNHDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<TaiKhoan?> RegisterAsync(RegisterDto request)
    {
        
        if (await _context.TAIKHOAN.AnyAsync(u => u.TenDangNhap == request.TenDangNhap))
        {
            return null;
        }

        var user = new TaiKhoan
        {
            TenDangNhap = request.TenDangNhap,
            MaNV = request.MaNV,
            Quyen = string.IsNullOrEmpty(request.Quyen) ? "NhanVien" : request.Quyen
        };

        
        var hashedPassword = new PasswordHasher<TaiKhoan>()
            .HashPassword(user, request.MatKhau);
        user.MatKhau = hashedPassword;

        _context.TAIKHOAN.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<string?> LoginAsync(LoginDto request)
    {
        var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.TenDangNhap == request.TenDangNhap);
        if (user == null)
            return null;

        var verifyResult = new PasswordHasher<TaiKhoan>()
            .VerifyHashedPassword(user, user.MatKhau, request.MatKhau);

        if (verifyResult == PasswordVerificationResult.Failed)
            return null;

        return CreateToken(user);
    }

    private string CreateToken(TaiKhoan user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.TenDangNhap),
            new Claim(ClaimTypes.Role, user.Quyen),
            new Claim(ClaimTypes.NameIdentifier, user.TenDangNhap)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:ExpireMinutes"] ?? "60")),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
