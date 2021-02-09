using System;
using System.Collections.Generic;
using System.Text;

namespace CryptographyProject
{
    class GeneralFile
    {

        private String name;
        private String path;
        private String realPath;
        private int depth;
        private GeneralFile parent;
        private UserInfo owner;

        public GeneralFile(string name, GeneralFile parent,UserInfo owner)
        {
            this.owner = owner;
            this.name = name;
            this.parent = parent;
            if (parent != null)
            {
                path += parent.path + @"\" + name;
                depth = parent.depth + 1;
            }
            else
            {
                path = name;
                depth = 0;
            }
            realPath = AppDomain.CurrentDomain.BaseDirectory + @"\" + path;
        }

        public int Depth { get => depth; set => depth = value; }
        public string Name { get => name; set => name = value; }
        public string Path { get => path; set => path = value; }
        public string RealPath { get => realPath; set => realPath = value; }
        internal GeneralFile Parent { get => parent; set => parent = value; }
        internal UserInfo Owner { get => owner; set => owner = value; }
    }
}
