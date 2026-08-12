namespace KIGHolding.Services;

public interface IBlogHtmlSanitizer
{
    string Sanitize(string html);
    bool ContainsMeaningfulContent(string html);
}
