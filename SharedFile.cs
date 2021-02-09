using System;
using System.Collections.Generic;
using System.Text;

namespace CryptographyProject
{
    class SharedFile : MyFile
    {
        private UserInfo user;

        public SharedFile(String name, GeneralFile parent, UserInfo owner, UserInfo user) : base(name, parent, owner)
        {
            this.user = user;
        }

        internal UserInfo User { get => user; set => user = value; }

        public override bool Equals(object obj)
        {
            return obj is SharedFile file &&
                   Name == file.Name &&
                   Owner.Username == file.Owner.Username &&
                   user.Username == file.user.Username;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Owner, user);
        }
    }
}
