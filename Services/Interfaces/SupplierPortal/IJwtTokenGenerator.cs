namespace DynamicFormService.DynamicFormServiceInterface;

public interface IJwtTokenGenerator
{
    string Generate(Guid companyId, bool isSlaSigned, string email, string companyName, out DateTime expiresAt);
}