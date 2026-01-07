namespace DynamicFormService.DynamicFormServiceInterface;

public interface IJwtTokenGenerator
{
    string Generate(Guid companyId, bool isSlaSigned, string email, string companyName, string isPasswordChanged,bool isacknowledge,DateTime? nextMeetingAt,
        out DateTime expiresAt);

}