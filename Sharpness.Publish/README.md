To generate BASE64 output from your .pem-file, run

```openssl base64 < key_name.pem | tr -d '\n' > key_name.pem.base64```
