namespace DataWin.Auth.Core.Enums;

public enum MfaMethod
{
    None = 0,
    Totp = 1,
    Sms = 2,
    Email = 3,
    WebAuthn = 4
}
