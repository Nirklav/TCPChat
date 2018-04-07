TCPChat
=======

![alt tag](https://raw.github.com/Nirklav/TCPChat/master/screen.png)

# Description:
Multi-user chat with mixed architecture: client-server, p2p. 
Server tightly works with certificates. Server and users should have certificates. But program can generate self signed certificates that supports too, but with some peculiarities.

First of all if server has self-signed certificate then users that connect to him will be warned.
Users also can use self-signed certificates, if them have it, then them will be marked with the appropriate icons in users list.
Also user can save certificates to local trusted TCPChat storage.

Here is this user icons:

  1. ![alt tag](https://raw.github.com/Nirklav/TCPChat/master/UI/Images/checked.png) This is approved certificate. This certificate is valid or it was saved in local TCPChat trusted certificates storage. Also user nick match to certificate common name.
  2. ![alt tag](https://raw.github.com/Nirklav/TCPChat/master/UI/Images/checkedNotMatch.png) This is also approved certificate, but nick does't match to certificate common name.
  3. ![alt tag](https://raw.github.com/Nirklav/TCPChat/master/UI/Images/notChecked.png) This is not approved self-signed certificate.

# Main idea:
  Main idea of this project - is multiple servers without databases. Where you can find friends and recognize that this is really them with he help of certificates.

# Server address: 
```
IP:   93.170.186.160
Port: 10021
Server certificate tumbprint: 839292da057678529acd42f414a51f8f8b16d1ff
```
Server certificate: [file](https://raw.github.com/Nirklav/TCPChat/master/ServerCertificate.cer)

# Supports:
  1. Full trafic encryption. Key exchange with certificates. AES-256 CBC.
  2. Private messages. (P2P)
  3. Rooms.
  4. Voice chat. (P2P)
  5. Files sharing. (P2P)
  6. Plugins (Example of simple plugin: https://github.com/Nirklav/ScreenshotPlugin).

P2P means that connection is established directly between clients without server.

OpenAL required for audio services. You can download it from [official site](https://www.openal.org/downloads/).
