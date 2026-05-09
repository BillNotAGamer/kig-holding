namespace KIGHolding.Services;

public class ContactCreateResult
{
    public Guid? ContactMessageId { get; init; }
    public IReadOnlyList<ContactServiceError> Errors { get; init; } = [];

    public bool Succeeded => ContactMessageId.HasValue && Errors.Count == 0;

    public static ContactCreateResult Success(Guid contactMessageId)
    {
        return new ContactCreateResult { ContactMessageId = contactMessageId };
    }

    public static ContactCreateResult Failed(IReadOnlyList<ContactServiceError> errors)
    {
        return new ContactCreateResult { Errors = errors };
    }
}

public class ContactServiceError
{
    public string FieldName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
