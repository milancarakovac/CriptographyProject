using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Security.Permissions;

namespace CryptographyProject
{
    class Program
    {
        static List<User> allUsers;
        static List<UserInfo> allUsersInfo;
        static User loggedUser;
        static MyFolder currentFolder;
        static MyFolder sharedFolder;
        static List<SharedFile> sharedFiles;
        static readonly string USERS_TXT = "" + AppDomain.CurrentDomain.BaseDirectory + "users.txt";
        static readonly string SHARED_TXT = "" + AppDomain.CurrentDomain.BaseDirectory + "shared.txt";
        static readonly string PATH_TXT = "" + AppDomain.CurrentDomain.BaseDirectory + "path.txt";
        static readonly string PASS_TXT = "" + AppDomain.CurrentDomain.BaseDirectory + "database/pass.txt";
        static readonly string DATABASE = "" + AppDomain.CurrentDomain.BaseDirectory + "database";
        static readonly string CA = "" + AppDomain.CurrentDomain.BaseDirectory + "CA";
        static string newFilePath;
        [STAThread]
        static void Main()
        {
            CollectAllUsers();
            Console.Clear();
            bool end = false;
            while (!end)
            {
                String input = Console.ReadLine();
                Console.Clear();
                switch (input)
                {
                    case "login":
                        {
                            if (loggedUser != null)
                            {
                                EnterWorkingSpace();
                                break;
                            }
                            Console.WriteLine("Unesite korisnicko ime:");
                            string username = Console.ReadLine();
                            Console.Clear();
                            Console.WriteLine("Unesite lozinku");
                            String password = ReadPassword();
                            Console.Clear();
                            if (username.Equals(String.Empty) && password.Equals(String.Empty))
                            {
                                ErrorMessage("Pogresan unos");
                                break;
                            }
                            if (!IsUserNameFree(username))
                            {
                                var values = generateHash(allUsersInfo.Find(x => x.Username.Equals(username)).HashAlgorithm, password, allUsersInfo.Find(x => x.Username.Equals(username)).Salt);
                                User user = new User(username, values[3].TrimEnd('\n'), values[2], values[1], allUsers.Find(x => x.Name.Equals(username)).CryptoAlgorithm, DATABASE + "/" + username + "/" + username + ".key");
                                if (Exists(user))
                                {
                                    if (!File.Exists(user.Certificate))
                                        ErrorMessage("Ne postoji sertifikat za odabranog korisnika");
                                    else
                                    {
                                        if (isCertificateValid(user.Certificate))
                                        {
                                            loggedUser = user;
                                            executeCommand("/C openssl rsa -in " + loggedUser.PrivateKeyFile + " -pubout -out " + loggedUser.UserInfo.PublicKey + " > errors.txt");
                                            Console.Clear();
                                            currentFolder = loggedUser.Root;
                                            CollectSharedFiles();
                                            EnterWorkingSpace();
                                        }
                                        else
                                            ErrorMessage("Sertifikat unesenog korisnika nije validan");
                                    }
                                }
                                else
                                {
                                    ErrorMessage("Pogresan unos");
                                }
                            }
                            else
                            {
                                ErrorMessage("Pogresan unos");
                            }
                            break;
                        }
                    case "end":
                        {
                            end = true;
                            SaveData();
                            break;
                        }
                    case "signup":
                        {
                            Console.WriteLine("Unesite novo korisnicko ime:");
                            String name = Console.ReadLine();
                            Console.Clear();
                            if (IsUserNameFree(name))
                            {
                                Console.WriteLine("Unesite lozinku: ");
                                String firstPassword = ReadPassword();
                                Console.Clear();
                                Console.WriteLine("Ponovite lozinku: ");
                                String secondPassword = ReadPassword();
                                Console.Clear();
                                if (firstPassword == secondPassword)
                                {
                                    Console.WriteLine("Odaberite hash algoritam koji zelite da koristite za hesiranje vase lozinke.\n SHA-256\n SHA-512\n md5");
                                    var hashType = Console.ReadLine();
                                    if (hashType == "md5") hashType = "1";
                                    else if (hashType == "SHA-512") hashType = "6";
                                    else if (hashType == "SHA-256") hashType = "5";
                                    else
                                    {
                                        ErrorMessage("Koristice se podrazumijevani hash algoritam(md5)");
                                        hashType = "1";
                                    }
                                    Console.Clear();
                                    Console.WriteLine("Odaberite algoritam koji zelite da koristite za kriptovanje vasih fajlova.\n des\n des3\n aes\n rc4");
                                    var algorithm = Console.ReadLine();
                                    if (algorithm != "aes" && algorithm != "des" && algorithm != "des3" && algorithm != "rc4")
                                    {
                                        ErrorMessage("Ovaj algoritam nije podrzan!Koristice se podrazumijevani algoritam za kriptovanje(aes)");
                                        algorithm = "aes-128-cbc";
                                    }
                                    if (algorithm == "aes")
                                        algorithm = "aes-128-cbc";
                                    var values = generateHash(hashType, secondPassword, "");
                                    User user = new User(name, values[3].TrimEnd('\n'), values[2], hashType, algorithm, "");
                                    allUsers.Add(user);
                                    allUsersInfo.Add(user.UserInfo);
                                    SaveData();
                                }
                                else
                                {
                                    ErrorMessage("Lozinke se ne poklapaju");
                                }
                            }
                            else
                            {
                                ErrorMessage("Korisnicko ime je zauzeto");
                            }
                            break;
                        }
                    case "shared":
                        {
                            WriteHeading();
                            foreach (var file in sharedFiles)
                            {
                                if(!file.Name.Contains("messageFor#") && !file.Name.Contains(".otisak"))
                                Console.WriteLine("{0, -15} {1,-15} {2,-15}", file.Name, file.Owner.Username, file.User.Username);
                            }
                            break;
                        }
                    case "users":
                        {
                            Console.WriteLine("Korisnicko ime:");
                            foreach (var user in allUsersInfo)
                                Console.WriteLine((allUsersInfo.IndexOf(user) + 1) + ". " + user.Username);
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            SaveData();
        }

        private static bool isCertificateValid(string certificate)
        {
            string cert = CA + "/certs/cert.txt";
            executeCommand("/C openssl x509 -in " + certificate + " -noout -dates > " + cert);
            var lines = File.ReadAllLines(cert);
            File.Delete(cert);
            var myLine = lines[1];
            string month = myLine.Split("=")[1].Split("  ")[0];
            string day = myLine.Split("=")[1].Split("  ")[1].Split(" ")[0];
            string year = myLine.Split("=")[1].Split("  ")[1].Split(" ")[2];
            DateTime date = new DateTime(Int32.Parse(year), (int)(Month.Feb + 1), Int32.Parse(day));
            var today = DateTime.Today;
            if (today.Year > date.Year)
                return false;
            else if (today.Month > date.Month)
                return false;
            else if (today.Day > date.Day)
                return false;
            else
                return true;
        }

        private static string[] generateHash(string hashType, string password, string salt)
        {
            string command = "/C openssl passwd -" + hashType;
            if (salt.Length == 0) command += " " + password;
            else command += " -salt " + salt + " " + password;
            return executeCommand(command).Split("$");
        }

        private static string executeCommand(String command)
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo("cmd.exe", command);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();
            StringBuilder q = new StringBuilder();
            while (!process.HasExited)
            {
                q.Append(process.StandardOutput.ReadToEnd());
            }
            return q.ToString();
        }

        private static void SaveData()
        {
            File.WriteAllText(USERS_TXT, "");
            foreach (var user in allUsers)
            {
                DirectoryInfo dir = null;
                if (!Directory.Exists(DATABASE + @"/" + user.Name))
                    dir = Directory.CreateDirectory(DATABASE + @"/" + user.Name);
                else
                    dir = new DirectoryInfo(DATABASE + @"/" + user.Name);
                var file = File.OpenWrite(DATABASE + "/" + user.Name + "/" + user.Name + ".txt");
                var text = user.Name + "\n" + user.Password + "\n" + user.Salt + "\n" + user.HashAlgorithm + "\n" + user.CryptoAlgorithm;
                file.Write(Encoding.ASCII.GetBytes(text));
                file.Close();
                var list = new List<string>();
                list.Add(user.Name);
                File.AppendAllLines(USERS_TXT, list);
            }
            if (sharedFiles.Count > 0)
            {
                var shared = File.OpenWrite(SHARED_TXT);
                foreach (var sharedFile in sharedFiles)
                {
                    var text = sharedFile.Name + " " + sharedFile.Owner.Username + " " + sharedFile.User.Username + "\n";
                    shared.Write(Encoding.ASCII.GetBytes(text));
                }
                shared.Close();
            }
        }

        private static void WriteAllFiles(bool isShared)
        {
            Console.Clear();
            if (isShared)
            {
                Console.WriteLine("{0, -15} {1, -15}", "naziv", "vlasnik");
                foreach (var file in currentFolder.Childrens)
                    if (!file.Name.EndsWith(".otisak") && !file.Name.Contains("messageFor#"))
                        Console.WriteLine("{0, -15} {1, -15}", file.Name, file.Owner.Username);
            }
            else
            {
                Console.WriteLine("{0, -15}", "naziv");
                foreach (var f in currentFolder.Childrens)
                    Console.WriteLine("{0, -15} {1, -15}", f.Name, f is MyFolder ? "folder" : "");
            }
            Console.ReadLine();
        }

        private static bool IsUserNameFree(string username)
        {
            foreach (var user in allUsersInfo)
                if (user.Username == username) return false;
            return true;
        }

        private static string ReadPassword()
        {
            String password = string.Empty;
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Backspace && password.Length > 0)
                {
                    Console.Write("\b \b");
                    password = password[0..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    Console.Write("*");
                    password += keyInfo.KeyChar;
                }
            } while (key != ConsoleKey.Enter);
            return password;
        }

        private static bool verifyFile(MyFile file)
        {
            var output = executeCommand("/C openssl dgst -verify " + file.Owner.PublicKey + " -signature " + file.RealPath + ".otisak " + file.RealPath + " 2>errors.txt");
            if (output.Contains("Verified OK"))
                return true;
            else return false;
        }
        private static bool signFile(MyFile file)
        {
            var output = executeCommand("/C openssl dgst -out " + file.RealPath + ".otisak -sign " + loggedUser.PrivateKeyFile + " " + file.RealPath);
            return true;
        }
        private static bool decryptFile(MyFile file)
        {
            var name = file.Name;
            var extension = name.Split(".")[name.Split(".").Length - 1];
            if (currentFolder.Name.Equals("shared"))
            {
                if (!verifyFile(file))
                    return false;
            }
            string exe = "/C openssl " + file.Owner.CryptoAlgorithm + " -d -in " + file.RealPath + " -out " + file.RealPath + "." + extension + " 2>errors.txt";
            executeCommand(exe);
            var error = executeCommand("/C cat errors.txt");
            if (!error.Contains("bad decrypt"))
            {
                executeCommand("/C " + file.RealPath + "." + extension);
                File.Delete(file.RealPath + "." + extension);
                return true;
            }
            File.Delete(file.RealPath + "." + extension);
            return false;
        }
        private static bool encryptFile(MyFile file, string name)
        {
            string command = "/C openssl " + loggedUser.CryptoAlgorithm + " -in " + file.RealPath + " -out " + file.Parent.RealPath + @"/" + name + " 2>errors.txt";
            var output = executeCommand(command);
            var error = executeCommand("/C cat errors.txt");
            if (error.Contains("bad password read"))
            {
                File.Delete(file.RealPath);
                var nothing = executeCommand("/C echo > errors.txt");
                return false;
            }
            else
            {
                File.Delete(file.RealPath);
                file.File = File.OpenRead(file.Parent.RealPath + @"/" + name);
                file.Name = name;
                file.RealPath = file.Parent.RealPath + "/" + name;
                file.File.Close();
                File.ReadAllLines(file.RealPath);
                return true;
            }
        }

        private static string decryptMessage(String path)
        {
            string message = "";
            MyFile file = (MyFile)sharedFolder.Childrens.Find(x => x.RealPath.Equals(path));
            if (!verifyFile(file))
            {
                return message;
            }
            else
                message = executeCommand("/C cat " + file.RealPath);
            return message;
        }

        private static void EnterWorkingSpace()
        {
            bool end = false;
            while (!end)
            {
                Console.Write(currentFolder.Path + ": ");
                String fullInput = Console.ReadLine();
                List<String> list = new List<String>(fullInput.Split(" "));
                String command = list[0];
                switch (command)
                {
                    case "logout":
                        {
                            end = true;
                            loggedUser = null;
                            currentFolder = null;
                            break;
                        }
                    case "list":
                        {
                            if (currentFolder.Name.Equals("shared"))
                                WriteAllFiles(true);
                            else
                                WriteAllFiles(false);
                            break;
                        }
                    case "makefile":
                        {
                            if (list.Count < 2)
                                ErrorMessage("Nedovoljan broj argumenata");
                            else
                            {
                                if (!currentFolder.Name.Equals("shared"))
                                {
                                    if (list[1].Contains("#"))
                                        ErrorMessage("Naziv fajla sadrzi nedozvoljen karakter");
                                    else if (currentFolder.Childrens.Find(x => x.Name.Equals(list[1])) != null)
                                        ErrorMessage("Fajl sa ovim imenom vec postoji");
                                    else
                                        MakeNewFile(list[1]);
                                }
                                else
                                    ErrorMessage("Ne mozete kreirati fajl u ovom folderu");
                            }
                            break;
                        }
                    case "makefolder":
                        {
                            if (list.Count < 2)
                                ErrorMessage("Nedovoljan broj argumenata");
                            else
                            {
                                if (list[1] == "shared")
                                    ErrorMessage("Ne mozete kreirati folder sa ovim imenom");
                                else
                                {
                                    if (!currentFolder.Name.Equals("shared"))
                                    {
                                        if (list[1].Contains(" "))
                                            ErrorMessage("Folder sadrzi nedozvoljen karakter");
                                        else
                                            MakeNewFolder(list[1]);
                                    }
                                    else
                                        ErrorMessage("Ne mozete kreirati folder u ovom folderu");
                                }
                            }
                            break;
                        }
                    case "delete":
                        {
                            if (list.Count < 2)
                                ErrorMessage("Nedovoljan broj argumenata");
                            else
                            {
                                if (!currentFolder.Name.Equals("shared"))
                                {
                                    if (currentFolder.Childrens.Find(x => x.Name.Equals(list[1])) != null)
                                    {
                                        MyFile file = (MyFile)currentFolder.Childrens.Find(x => x.Name.Equals(list[1]));
                                        currentFolder.RemoveChildren(file);
                                        if (IsShared(file))
                                            RemoveFromShared(file);
                                    }
                                    else
                                        ErrorMessage("Ne postoji fajl sa unesenim imenom");
                                }
                                else
                                    ErrorMessage("Ne mozete brisati fajlove u ovom folderu");
                            }
                            break;
                        }
                    case "upload":
                        {
                            if (!currentFolder.Name.Equals("shared"))
                            {
                                var thread = new Thread(OpenFileDialog) { IsBackground = true };
                                thread.SetApartmentState(ApartmentState.STA);
                                thread.Start();
                                thread.Join();
                            }
                            else
                                ErrorMessage("Ne mozete dodavati fajlove u ovom folderu");
                            break;
                        }
                    case "download":
                        {
                            if (!currentFolder.Name.Equals("shared"))
                            {
                                if (list.Count < 2)
                                    ErrorMessage("Nedovoljan broj argumenata");
                                else
                                {
                                    var file = currentFolder.Childrens.Find(x => x.Name.Equals(list[1]));
                                    if (file != null)
                                    {
                                        if (!File.Exists(newFilePath + file.Name))
                                            decryptAndDownload(file, newFilePath);
                                        else
                                            ErrorMessage("Fajl vec postoji");
                                    }
                                    else
                                        ErrorMessage("Pogresan unos");
                                }
                            }
                            else
                                ErrorMessage("Ne mozete skinuti fajl iz ovog foldera");
                            break;
                        }
                    case "share":
                        {
                            if (!currentFolder.Name.Equals("shared"))
                            {
                                if (list.Count < 3)
                                    ErrorMessage("Nedovoljan broj argumenata");
                                else
                                {
                                    if (!IsUserNameFree(list[2]))
                                    {
                                        if (DoesFileExists(list[1]))
                                        {
                                            var destPath = sharedFolder.RealPath + @"\" + list[2] + "#" + list[1];
                                            if (!File.Exists(destPath))
                                            {
                                                Console.Clear();
                                                Console.WriteLine("Unesite zastitnu lozinku ovog fajla:");
                                                var password = Console.ReadLine();
                                                MyFile message = new MyFile("messageFor#" + list[2] + "#about#" + list[1].Split(".")[0] + "$" + list[1].Split(".")[1] + ".txt", sharedFolder, loggedUser.UserInfo);
                                                File.WriteAllText(message.RealPath, password);
                                                signFile(message);
                                                sharedFolder.AddChildren(message);
                                                File.Copy(currentFolder.RealPath + @"\" + list[1], destPath);
                                                var file = new SharedFile(list[2] + "#" + list[1], sharedFolder, allUsersInfo.Find(x => x.Username.Equals(loggedUser.Name)), allUsersInfo.Find(x => x.Username.Equals(list[2])));
                                                signFile(file);
                                                sharedFolder.AddChildren(file);
                                                sharedFiles.Add(file);
                                                SaveData();
                                            }
                                            else
                                                ErrorMessage("Fajl je vec podjeljen sa odabranim korisnikom");
                                        }
                                        else
                                            ErrorMessage("Odabrani fajl ne postoji");
                                    }
                                    else
                                        ErrorMessage("Ne postoji korisnik sa unesenim imenom");
                                }
                            }
                            else
                                ErrorMessage("Ne mozete dijeliti fajlove iz ovog foldera");
                            break;
                        }
                    case "movetoshared":
                        {
                            CollectSharedFiles();
                            currentFolder = sharedFolder;
                            break;
                        }
                    case "exitshared":
                        {
                            currentFolder = loggedUser.Root;
                            break;
                        }
                    case "listshared":
                        {
                            Console.WriteLine("Podijeljeni fajlovi(i lozinka za otvaranje):");
                            foreach (string f in Directory.GetFiles(sharedFolder.RealPath))
                            {
                                if (f.Contains("messageFor#") && !f.Contains(".otisak") && f.Contains(loggedUser.Name))
                                {
                                    var fileName = f.Split(@"\")[f.Split(@"\").Length - 1];
                                    var firstPart = fileName.Split("$")[0];
                                    var secondPart = fileName.Split("$")[1];
                                    var fname = firstPart.Split("#")[firstPart.Split("#").Length - 1];
                                    var extension = secondPart.Split(".")[0];
                                    var message = decryptMessage(f);
                                    Console.WriteLine(fname + "." + extension + "  --->  " + message);
                                }
                            }
                            Console.ReadLine();
                            break;
                        }
                    case "edit":
                        {
                            if (list.Count < 2)
                                ErrorMessage("Nedovoljan broj argumenata");
                            else
                            {
                                if (DoesFileExists(list[1]))
                                {
                                    if (list[1].EndsWith(".txt"))
                                    {
                                        Console.Clear();
                                        var file = currentFolder.Childrens.Find(x => x.Name.EndsWith(list[1]));
                                        executeCommand("/C openssl " + file.Owner.CryptoAlgorithm + " -d -in " + file.RealPath + " -out error.txt" + " 2>errors.txt");
                                        var error = executeCommand("/C cat errors.txt");
                                        if(error.Contains("bad decrypt"))
                                        {
                                            ErrorMessage("Pogresna lozinka");
                                            break;
                                        }
                                        Console.WriteLine("Unesite novi sadrzaj fajla!");
                                        var content = Console.ReadLine();
                                        currentFolder.EditChildren(currentFolder.Childrens.Find(x => x.Name.EndsWith(list[1])), content);
                                        executeCommand("/C openssl " + loggedUser.CryptoAlgorithm + " -in " + file.RealPath + " -out " + file.RealPath + ".txt");
                                        File.Delete(file.RealPath);
                                        File.Copy(file.RealPath + ".txt", file.RealPath);
                                        File.Delete(file.RealPath + ".txt");
                                    }
                                    else
                                        ErrorMessage("Tip fajla nije podrzan");
                                }
                                else
                                    ErrorMessage("Fajl sa ovim nazivom ne postoji");
                            }
                            break;
                        }
                    case "move":
                        {
                            if (list.Count < 2)
                                ErrorMessage("Nedovoljan broj argumenata");
                            else
                            {
                                if (DoesFolderExists(list[1]))
                                    currentFolder = (MyFolder)currentFolder.Childrens.Find(x => x.Name.Equals(list[1]));
                                else
                                    ErrorMessage("Izabrani folder ne postoji");
                            }
                            break;
                        }
                    case "back":
                        {
                            if (currentFolder.Depth > 0)
                                currentFolder = (MyFolder)currentFolder.Parent;
                            break;
                        }
                    default:
                        string name = list[0];
                        {
                            if (DoesFileExists(name))
                            {
                                if (name.Contains("."))
                                {
                                    if (!decryptFile((MyFile)currentFolder.Childrens.Find(x => x.Name.Equals(name))))
                                        ErrorMessage("Ne mozete otvoriti ovaj fajl");
                                }
                                else
                                    ErrorMessage("Ovaj tip fajla ne moze da se otvori");
                            }
                            else
                                ErrorMessage("Nepoznata operacija");
                            break;
                        }
                }
                Console.Clear();
            }
        }

        private static void decryptAndDownload(GeneralFile file, string newFilePath)
        {
            string exe = "/C openssl " + file.Owner.CryptoAlgorithm + " -d -in " + file.RealPath + " -out " + newFilePath + "/" + file.Name + " 2>errors.txt";
            executeCommand(exe);
            var error = executeCommand("/C cat errors.txt");
            if (error.Contains("bad decrypt"))
            {
                File.Delete(newFilePath + "/" + file.Name);
                ErrorMessage("Neuspjesna dekripcija");
            }
        }

        private static void RemoveFromShared(MyFile file)
        {
            SharedFile thatFile = null;
            foreach (var f in sharedFiles)
                if (f.Name.Equals(file.Name) && f.Owner.Equals(loggedUser.UserInfo))
                    thatFile = f;
            sharedFiles.Remove(thatFile);
            File.Delete(thatFile.RealPath);
        }

        private static bool IsShared(MyFile file)
        {
            foreach (var f in sharedFiles)
                if (f.Name.Equals(file.Name) && f.Owner.Equals(loggedUser.UserInfo))
                    return true;
            return false;
        }

        [STAThread]
        private static void OpenFileDialog()
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = false,
                Title = "Ucitavanje fajla sa hosta"
            };
            using (dialog)
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var data = dialog.FileName.Split(@"\");
                    string name = data[^1];
                    var newFile = new MyFile("name" + name, loggedUser.UploadedFiles, allUsersInfo.Find(x => x.Username.Equals(loggedUser.Name)));
                    if (((MyFolder)loggedUser.Root.Childrens.Find(x => x.Name.Equals("uploaded"))).Childrens.Find(x => x.Name.Equals(name)) != null)
                        ErrorMessage("Fajl sa istim imenom je vec ucitan");
                    else
                    {
                        File.WriteAllBytes(newFile.RealPath, File.ReadAllBytes(dialog.FileName));
                        if (encryptFile(newFile, name))
                        {
                            loggedUser.UploadedFiles.AddChildren(newFile);
                            ((MyFolder)loggedUser.Root.Childrens.Find(x => x.Name.Equals("uploaded"))).AddChildren(newFile);
                        }
                    }
                }
            }
        }

        private static void ErrorMessage(String message)
        {
            Console.Clear();
            Console.WriteLine(message + "!\nPritisnite enter za nastavak.");
            Console.ReadLine();
        }

        private static bool DoesFolderExists(String name)
        {
            foreach (var file in currentFolder.Childrens)
                if (file.Name.Equals(name) && (file is MyFolder))
                    return true;
            return false;
        }

        private static bool DoesFileExists(string name)
        {
            foreach (var file in currentFolder.Childrens)
                if (file.Name.Equals(name) && (file is MyFile))
                    return true;
            return false;
        }

        private static void WriteHeading()
        {
            Console.WriteLine("{0, -15} {1,-15} {2,-15}", "fajl", "vlasnik", "korisnik");
        }

        private static void MakeNewFolder(string name)
        {
            Console.Clear();
            currentFolder.AddChildren(new MyFolder(name, currentFolder, loggedUser.UserInfo));
        }

        private static void MakeNewFile(String name)
        {
            Console.Clear();
            MyFile file = new MyFile("name" + name, currentFolder, allUsersInfo.Find(x => x.Username.Equals(loggedUser.Name)));
            if (name.EndsWith(".txt"))
            {
                Console.WriteLine("Unesite sadrzaj fajla: ");
                var content = Console.ReadLine();
                file.Edit(content);
            }
            if (encryptFile(file, name))
            {
                currentFolder.AddChildren(file);
            }
        }

        private static bool Exists(User user)
        {
            if (allUsers != null)
            {
                foreach (User u in allUsers)
                    if (u.Equals(user)) return true;
            }
            return false;
        }

        private static void CollectAllUsers()
        {
            var allLines = File.ReadAllLines(USERS_TXT);
            allUsers = new List<User>();
            allUsersInfo = new List<UserInfo>();
            sharedFolder = new MyFolder("shared", null, null);
            sharedFiles = new List<SharedFile>();
            foreach (var line in allLines)
            {
                var allData = File.ReadAllLines(DATABASE + "/" + line + "/" + line + ".txt");
                var user = new User(allData[0], allData[1], allData[2], allData[3], allData[4], DATABASE + "/" + line + "/" + allData[0] + ".key");
                allUsersInfo.Add(user.UserInfo);
                allUsers.Add(user);
            }
            var paths = File.ReadAllLines(PATH_TXT);
            if (paths.Length > 0)
                newFilePath = paths[0];
            else
                newFilePath = @"C:\Users\";
            CollectSharedFiles();
        }

        private static void CollectSharedFiles()
        {
            var lines = File.ReadAllLines(SHARED_TXT);
            foreach (string f in Directory.GetFiles(sharedFolder.RealPath))
            {
                foreach (var line in lines)
                {
                    var data = line.Split(" ");
                    if (data.Length > 2)
                    {
                        var filename = Path.GetFileName(f);
                        if (sharedFiles.Find(x => x.Name.Equals(filename)) == null)
                        {
                            var newfile = new SharedFile(filename, sharedFolder, allUsersInfo.Find(x => x.Username.Equals(data[1])), allUsersInfo.Find(x => x.Username.Equals(data[2])));
                            sharedFolder.AddChildren(newfile);
                            sharedFiles.Add(newfile);
                        }
                    }
                }
            }
        }
    }
}
enum Month{
    Jan, Feb, Mar, Apr, May, Jun, Jul , Aug, Sep, Oct, Nov, Dec
};