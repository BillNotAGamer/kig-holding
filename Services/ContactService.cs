using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Models.Enums;

namespace KIGHolding.Services;

public class ContactService : IContactService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ContactService> _logger;

    public ContactService(AppDbContext dbContext, ILogger<ContactService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ContactCreateResult> CreateContactMessageAsync(ContactCreateRequest request, CancellationToken cancellationToken = default)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return ContactCreateResult.Failed(errors);
        }

        var now = DateTimeOffset.UtcNow;
        var contactMessage = new ContactMessage
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Subject = ContactSubjectOptions.NormalizeOrNull(request.Subject),
            Message = request.Message.Trim(),
            Status = ContactMessageStatus.New,
            CreatedAt = now,
            UpdatedAt = now
        };

        try
        {
            _dbContext.ContactMessages.Add(contactMessage);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to save contact message.");
            return ContactCreateResult.Failed(
            [
                new ContactServiceError
                {
                    FieldName = string.Empty,
                    Message = "Không thể lưu thông tin liên hệ lúc này. Vui lòng thử lại hoặc gọi hotline để được hỗ trợ."
                }
            ]);
        }

        return ContactCreateResult.Success(contactMessage.Id);
    }

    private static IReadOnlyList<ContactServiceError> Validate(ContactCreateRequest request)
    {
        var errors = new List<ContactServiceError>();

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            errors.Add(new ContactServiceError
            {
                FieldName = nameof(request.FullName),
                Message = "Vui lòng nhập họ tên."
            });
        }

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            errors.Add(new ContactServiceError
            {
                FieldName = nameof(request.PhoneNumber),
                Message = "Vui lòng nhập số điện thoại."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            errors.Add(new ContactServiceError
            {
                FieldName = nameof(request.Message),
                Message = "Vui lòng nhập nội dung liên hệ."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            errors.Add(new ContactServiceError
            {
                FieldName = nameof(request.Subject),
                Message = "Vui lòng chọn chủ đề."
            });
        }
        else if (!ContactSubjectOptions.TryNormalize(request.Subject, out _))
        {
            errors.Add(new ContactServiceError
            {
                FieldName = nameof(request.Subject),
                Message = "Chủ đề liên hệ không hợp lệ."
            });
        }

        return errors;
    }
}
