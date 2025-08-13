using Microsoft.AspNetCore.Identity;

public interface IFirmRepository
{
    Task<IdentityResult> DeleteFirmWithDependenciesAsync(int firmId);
}