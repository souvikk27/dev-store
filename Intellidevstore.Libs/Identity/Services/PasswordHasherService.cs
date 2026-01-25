namespace Intellidevstore.Libs.Identity.Services;

public class PasswordHasherService : IPasswordHasherService
{
    public string HashPassword(string password)
    {
        // Generate a random salt and hash the password using BCrypt
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        // Verify the password against the hash using BCrypt
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }
}
