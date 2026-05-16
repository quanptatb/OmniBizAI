using Microsoft.AspNetCore.DataProtection;

namespace OmniBizAI.Helpers
{
    /// <summary>
    /// Provides data protection (encryption/decryption) for sensitive values such as SMTP passwords.
    /// </summary>
    public class EncryptionHelper
    {
        private readonly IDataProtector _protector;

        public EncryptionHelper(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("OmniBizAI.EncryptionPurpose");
        }

        public string Encrypt(string plainText) => _protector.Protect(plainText);

        public string Decrypt(string cipherText) => _protector.Unprotect(cipherText);
    }
}
