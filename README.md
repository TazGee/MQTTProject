#👨‍💻 **RestAPI Backend Projekat** 👨‍💻
### RestAPI Backend Projekat za komunikaciju putem MQTT protokola napravljen u .NET-u.
**Programski jezik:** C#
**Tehnologije:** .NET, Docker, MariaDB, Entity Framework
**Okruzenja:** Rider, MySQL Workbench
\
\
##🖥️ **Features** 🖥️
- Prijavljivanje na odredjeni topic po zelji
- Pracenje poruka koje stizu na taj topic
- Preuzimanje svih poruka koje su ikada poslate
- Automatski resubscribe na sve topic-e koji su bili praceni pre disconnect-a
- Pregled statistike svih (i pojedinacnih) topic-a
  \
  \
  ##📀 **Prerequisites** 📀
- .NET SDK 8
- Docker
- MQTT Broker
- Swagger (za testiranje)
  \
  \
  ##🧑‍💻 **Setup Instrukcije** 🧑‍💻
- Neophodno je prethodno kloniranje git repozitorijuma
- U folderu 'MQTTRestApi' neophodno je popuniti .env fajl (primer u .env.example)
- Unutar foldera MQTTRestApi pritisnuti desni klik > Open in Terminal
- Ukucati docker-compose up -d --build
- Ukoliko nema gresaka, API je spreman za upotrebu
  \
  \
  ##🌐 **API Endpoints** 🌐
- **(POST)api/mqtt/subscribe**
   - Input parametri:
      - Topic (string)
   - Vrsi pretplatu na neki topic
   - Podrzava single i multi level wildcards
- **(POST)api/mqtt/publish**
   - Input parametri:
      - Topic (string)
      - Payload (string)
      - QoS (int32)
   - Sluzi za slanje poruke na neki topic
- **(GET)api/mqtt/messages**
   - Input parametri:
      - Nema input parametre
   - Vraca listu svih poruka iz baze podataka
- **(GET)api/mqtt/stats**
   - Input parametri:
      - Nema input parametre
   - Vraca broj koliko puta je bila poslata poruka na svaki od topica
- **(GET)api/mqtt/stats/{topic}**
   - Input parametri:
      - Topic (string)
   - Vraca broj koliko puta je bila poslata poruka na odredjen topic
     \
     \
     ##⌨️ **Primeri curl komandi** ⌨️
- Subscribe
```bash
curl -X 'POST' \
  'http://localhost:5000/api/mqtt/subscribe' \
  -H 'accept: */*' \
  -H 'Content-Type: application/json' \
  -d '{
  "topic": "kuca/kupatilo/temperatura"
}'
```
- Publish
```bash
curl -X 'POST' \
  'http://localhost:5000/api/mqtt/publish' \
  -H 'accept: */*' \
  -H 'Content-Type: application/json' \
  -d '{
  "topic": "kuca/kupatilo/temperatura",
  "payload": "22",
  "qoS": 0
}'
```
- Lista poruka
```bash
curl -X 'GET' \
  'http://localhost:5000/api/mqtt/messages' \
  -H 'accept: */*'
```
- Stats za sve topice
```bash
curl -X 'GET' \
  'http://localhost:5000/api/mqtt/stats' \
  -H 'accept: */*'
```
- Stats za jedan topic
```bash
curl -X 'GET' \
  'http://localhost:5000/api/mqtt/stats/kuca%2Fsoba%2Ftemperatura' \
  -H 'accept: */*'
```
\
\
##🎛️ **Testing instrukcije** 🎛️
- Za pokretanje Unit testova neophodno je:
- Otvoriti folder `MqttRestApi.Tests`
- Pritisnuti desni klik > Open in Terminal
- ukucati:
```bash 
    dotnet test
```
\
\
##📃 **Licenca** 📃
- 