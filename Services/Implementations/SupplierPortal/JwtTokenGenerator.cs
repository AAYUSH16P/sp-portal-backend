using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DynamicFormService.DynamicFormServiceInterface;
using Microsoft.Extensions.Configuration;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _config;

    public JwtTokenGenerator(IConfiguration config)
    {
        _config = config;
    }

    public string Generate(Guid companyId, bool isSlaSigned,string email,string companyName,string isPasswordChanged, out DateTime expiresAt)
    {
        expiresAt = DateTime.UtcNow.AddMinutes(20);

        var claims = new[]
        {
            new Claim("companyId", companyId.ToString()),
            new Claim("isSlaSigned", isSlaSigned.ToString()),
            new Claim("email", email.ToString()),
            new Claim("companyName", companyName.ToString()),
            new Claim("isPasswordChanged", isPasswordChanged)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}