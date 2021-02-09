username="milan"
openssl req -new -key ../database/$username/$username.key -out requests/$username.csr -config openssl.cnf

openssl ca -in requests/$username.csr -out certs/$username.pem -config openssl.cnf