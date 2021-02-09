sadrzaj=`cat users.txt`
korisnici=${sadrzaj}
for korisnik in $korisnici
do
	rm -r $korisnik
	rm CA/certs/$korisnik.pem
	rm CA/requests/$korisnik.csr
done
rm -r CA/newcerts
mkdir CA/newcerts
rm CA/index.txt
touch CA/index.txt
echo 01 > CA/serial
rm -r shared
mkdir shared
rm shared.txt
touch shared.txt
rm users.txt
touch users.txt
rm -r database
mkdir database
