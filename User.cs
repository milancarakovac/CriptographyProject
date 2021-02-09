using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace CryptographyProject
{
    class User
    {

        private String name;
        private String password;
        private String salt;
        private String hashAlgorithm;
        private String cryptoAlgorithm;
        private MyFolder root;
        private MyFolder uploadedFiles;
        private UserInfo userInfo;
        private String privateKeyFile;
        private String certificate;

        public User(string name, string password, string salt, string hashAlgorithm, string cryptoAlgorithm, string key)
        {
            UserInfo = new UserInfo(name, "" + AppDomain.CurrentDomain.BaseDirectory + "database" + "/" + name + "/public-" + name + ".key", hashAlgorithm, salt);
            certificate = "" + AppDomain.CurrentDomain.BaseDirectory + "CA/certs/" + name + ".pem";
            this.userInfo.CryptoAlgorithm = cryptoAlgorithm;
            this.name = name;
            this.password = password;
            this.salt = salt;
            this.hashAlgorithm = hashAlgorithm;
            this.cryptoAlgorithm = cryptoAlgorithm;
            if (key.Length > 0)
                this.privateKeyFile = key;
            else
            {
                this.privateKeyFile = "" + AppDomain.CurrentDomain.BaseDirectory + "database" + "/" + name + "/" + name + ".key";
                Process.Start("cmd.exe", "/C openssl genrsa -out " + privateKeyFile + " 2048");
            }
            CollectExistingFiles();
        }

        private void CollectExistingFiles()
        {
            if (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + name))
            {
                root = new MyFolder(Name, null, userInfo);
                UploadedFiles = new MyFolder("uploaded", root, userInfo);
                DirectorySearch(root);
            }
            else
            {
                root = new MyFolder(name, null, userInfo);
                UploadedFiles = new MyFolder("uploaded", root, userInfo);
            }
        }

        private void DirectorySearch(MyFolder dir)
        {

            foreach (string f in Directory.GetFiles(dir.RealPath))
            {
                var file = new MyFile(Path.GetFileName(f), dir, UserInfo);
                dir.AddChildren(file);
            }
            foreach (string d in Directory.GetDirectories(dir.RealPath))
            {
                var folder = new MyFolder(Path.GetFileName(d), dir, userInfo);
                dir.AddChildren(folder);
                if (!folder.Name.Equals("shared"))
                    DirectorySearch(folder);
            }

        }

        public string Name { get => name; set => name = value; }
        public string Password { get => password; set => password = value; }
        internal MyFolder Root { get => root; set => root = value; }
        internal MyFolder UploadedFiles { get => uploadedFiles; set => uploadedFiles = value; }
        internal UserInfo UserInfo { get => userInfo; set => userInfo = value; }
        public string Salt { get => salt; set => salt = value; }
        public string HashAlgorithm { get => hashAlgorithm; set => hashAlgorithm = value; }
        public string CryptoAlgorithm { get => cryptoAlgorithm; set => cryptoAlgorithm = value; }
        public string PrivateKeyFile { get => privateKeyFile; set => privateKeyFile = value; }
        public string Certificate { get => certificate; set => certificate = value; }

        public override bool Equals(object obj)
        {
            return obj is User user && name == user.name && password == user.password;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(name, password);
        }
    }
}
