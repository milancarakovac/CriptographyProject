using System;
using System.Collections.Generic;
using System.Text;

namespace CryptographyProject
{
    class UserInfo
    {
        private string username;
        private string publicKey;
        private string hashAlgorithm;
        private string salt;
        private string cryptoAlgorithm;

        public UserInfo(string username, string publicKey, string hashAlgorithm, string salt)
        {
            this.username = username;
            this.publicKey = publicKey;
            this.hashAlgorithm = hashAlgorithm;
            this.salt = salt;
            this.cryptoAlgorithm = "";
        }
        public string PublicKey { get => publicKey; set => publicKey = value; }
        public string Username { get => username; set => username = value; }
        public string HashAlgorithm { get => hashAlgorithm; set => hashAlgorithm = value; }
        public string Salt { get => salt; set => salt = value; }
        public string CryptoAlgorithm { get => cryptoAlgorithm; set => cryptoAlgorithm = value; }
    }
}
