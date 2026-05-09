using DataWin.Auth.Core.ValueObjects;

namespace DataWin.Auth.Core.Interfaces.Services;

public interface IPasswordHasher
{
    HashedPassword Hash(string password);
    bool Verify(string password, HashedPassword hash);
}
