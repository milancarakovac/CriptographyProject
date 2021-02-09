using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CryptographyProject
{
    class MyFolder : GeneralFile
    {

        private DirectoryInfo directoryInfo;
        private List<GeneralFile> childrens;

        public MyFolder(String name, GeneralFile parent, UserInfo owner) : base(name, parent, owner)
        {
            if (!Directory.Exists(RealPath))
            {
                directoryInfo = Directory.CreateDirectory(RealPath);
            }
            else
            {
                directoryInfo = new DirectoryInfo(RealPath);
            }
            childrens = new List<GeneralFile>();
        }

        public void AddChildren(GeneralFile child)
        {
            childrens.Add(child);
        }

        public void RemoveChildren(GeneralFile child)
        {
            childrens.Remove(child);
            File.Delete(child.RealPath);
        }

        public void EditChildren(GeneralFile child, String newContent)
        {
            GeneralFile file = childrens.Find(r => r.Name.Equals(child.Name));
            if (file is MyFile file1)
            {
                file1.Edit(newContent);
            }
            else
            {
                Console.WriteLine(@"Fajl ne postoji!\nPritisnite enter za nastavak.");
                Console.ReadLine();
            }
        }

        public DirectoryInfo DirectoryInfo { get => directoryInfo; set => directoryInfo = value; }
        internal List<GeneralFile> Childrens { get => childrens; set => childrens = value; }
    }
}
