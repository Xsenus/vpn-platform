using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace VpnPlatform.Application.Common;

public static class TelegramNotificationDeduplication
{
    public static string CreateKey(long telegramUserId, string type, string payloadJson)
    {
        var identity = string.Concat(
            telegramUserId.ToString(CultureInfo.InvariantCulture),
            "\n",
            type.Trim(),
            "\n",
            payloadJson);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
