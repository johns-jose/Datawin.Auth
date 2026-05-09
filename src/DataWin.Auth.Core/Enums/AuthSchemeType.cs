namespace DataWin.Auth.Core.Enums;

public enum AuthSchemeType
{
    Internal = 0,
    OAuth2 = 1,
    OpenIdConnect = 2,
    Saml2 = 3,
    ExternalGoogle = 10,
    ExternalAzureAd = 11,
    ExternalOkta = 12
}
