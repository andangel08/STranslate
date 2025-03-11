using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace STranslate.Util;

public class ChatglmAuthenicationUtil
{
    // 生成Token
    public static string GenerateToken(string apiKey, int expSeconds)
    {
        // 将API key按照'.'进行分割
        var parts = apiKey.Split('.');
        if (parts.Length != 2) throw new ArgumentException("Invalid API key format.");
        // 获取API key中的id和secret
        var id = parts[0];
        var secret = parts[1];
        // 将secret转换为字节数组
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        // 如果字节数组长度小于32，则扩展字节数组长度
        if (keyBytes.Length < 32)
            // 扩展密钥以满足最小长度要求
            Array.Resize(ref keyBytes, 32);
        // 创建对称加密密钥
        var securityKey = new SymmetricSecurityKey(keyBytes);
        // 创建签名凭证
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        // 创建JWT负载
        var payload = new JwtPayload
        {
            { "api_key", id },
            { "exp", DateTimeOffset.UtcNow.ToUnixTimeSeconds() + expSeconds },
            { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
        };
        // 创建JWT头部
        var header = new JwtHeader(credentials)
        {
            { "sign_type", "SIGN" }
        };
        // 创建JWT
        var token = new JwtSecurityToken(header, payload);
        // 返回JWT
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}