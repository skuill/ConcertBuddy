using System.Text;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Helper
{
    public class StringHelper
    {
        public static string SubstringByByteLength(string message, int length)
        {
            if (string.IsNullOrEmpty(message) || Encoding.UTF8.GetByteCount(message) <= length)
            {
                return message;
            }

            var encoder = Encoding.UTF8.GetEncoder();
            byte[] buffer = new byte[length];
            encoder.Convert(message.AsSpan(), buffer.AsSpan(), false, out _, out int bytesUsed, out _);
            return Encoding.UTF8.GetString(buffer, 0, bytesUsed);
        }
    }
}
