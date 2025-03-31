namespace backend.Services
{
    public class PasswordGeneratorService
    {
        private static readonly string Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        
        public string GenerateTemporaryPassword(int length = 8)
        {
            var random = new Random();
            return new string(Enumerable.Repeat(Chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}