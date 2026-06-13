namespace KIGHolding.Services;

public static class AdminPasswordPolicy
{
    public static IReadOnlyList<string> Validate(string? password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Vui lòng nhập mật khẩu.");
            return errors;
        }

        if (password.Length < 12)
        {
            errors.Add("Mật khẩu phải có ít nhất 12 ký tự.");
        }

        if (!password.Any(char.IsLower))
        {
            errors.Add("Mật khẩu phải có ít nhất một chữ thường.");
        }

        if (!password.Any(char.IsUpper))
        {
            errors.Add("Mật khẩu phải có ít nhất một chữ hoa.");
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add("Mật khẩu phải có ít nhất một chữ số.");
        }

        if (!password.Any(character => !char.IsLetterOrDigit(character)))
        {
            errors.Add("Mật khẩu phải có ít nhất một ký tự đặc biệt.");
        }

        return errors;
    }
}
