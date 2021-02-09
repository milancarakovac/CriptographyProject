using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CryptographyProject
{
    class MyFile : GeneralFile
    {
        private FileStream file;

        public MyFile(String name, GeneralFile parent, UserInfo owner) : base(name, parent, owner)
        {
            file = System.IO.File.Open(RealPath, FileMode.OpenOrCreate);
            file.Close();
        }
        public MyFile(FileInfo file, GeneralFile parent, UserInfo owner) : base(file.Name, parent, owner)
        {
            this.file = System.IO.File.Open(file.FullName, FileMode.OpenOrCreate);
        }
        public void Edit(String content)
        {
            System.IO.File.WriteAllText(RealPath,content);
            file.Close();
        }

        public FileStream File { get => file; set => file = value; }
    }
}
