namespace Convy.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }
    string FirebaseUid { get; }
}
