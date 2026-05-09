namespace KIGHolding.Services;

public interface IContactService
{
    Task<ContactCreateResult> CreateContactMessageAsync(ContactCreateRequest request, CancellationToken cancellationToken = default);
}
