# 👨‍💻 **RestAPI Backend Projekat** 👨‍💻
### RestAPI Backend za komunikaciju putem MQTT protokola
**Verzija:** 0.26.10.12.1 by **Nikola Tasić**
\
**Programski jezik:** C#
\
**Tehnologije:** .NET, Docker, MariaDB, Entity Framework
\
**Okruzenja:** Rider, MySQL Workbench
## 📜 - **Najnovije izmene** - 📜️
### **-= Verzija 0.26.10.13.1 =-** 

- **User authentication:**
  - Dodato **hesiranje (hashing) lozinke** za vecu bezbednost
  - Dodata opcija **promene lozinke**
  - Dodata **administrator** opcija za **promenu role-a korisnika**
- **Nove Admin opcije:**
  - **Log list** - prikaz kompletnog log-a (svih log-ova iz baze)
  - **All messages** - prikaz svih log-ova sa svih topic-a
- **Poruke:**
  - Dodata opcija za filtrirani pregled poruka (filter po vremenu unazad u minutima)
- **Sistem alarmiranja:**
  - Dodat **sistem alarmiranja** koji prati vrednosti na odredjenim topics-ima
  - **Alarme** kreira **admin** i oni se za svaki topic cuvaju u bazi
  - Postoje 3 vrste alarma:
    - **Alarm** koji prati samo odstupanje vrednosti **ispod minimalne granice**
    - **Alarm** koji prati samo odstupanje vrednosti **iznad maksimalne granice**
    - **Alarm** koji prati odstupanje vrednosti **izvan obe granice**
  - Kada se **alarm aktivira** zapisuje se u **logu** i u samom **entitetu poruke**
  - Dodata nova kolona za **MqttMessage** - **AlarmType**
  - Dodata nova tabela - **Alarm**
- **Simulator:**
  - Dodata opcija da simulator cita iz konfiguracije zadatu vrednost koju treba da salje
  - Dodata opcija da se simulator automatski pretplati na "topic + /updateValue"
  - Preko gore navedenog topic-a admin moze simulatoru da menja vrednost preko brokera
  - Najduzi interval slanja produzen sa 10 na 60 sekundi
  - Dodato da admin moze da publishuje poruke na topic (endpoint)
  - Publishovanje poruka preko enpointa menja vrednost koju simulator daje
***
## 🖥️ - **Features** - 🖥️
- Pretplata backend-a na topic po zelji
- Korisnicka autentifikacija putem korisnickog imena i lozinke
- Vise korisnickih uloga
- Prikazivanje svih dostupnih topic-a
- Korisnicko pretplacivanje na topic po zelji
- Uklanjanje pretplate sa topic-a
- Pracenje poruka koje stizu na topic
- Preuzimanje svih poruka koje su poslate na odredjeni topic
- Automatski resubscribe na sve topic-e koji su bili praceni pre disconnect-a
- Pregled statistike svih (i pojedinacnih) topic-a
- Podesivo simuliranje uredjaja koji salje poruke
- Sistem za automatski reconnect prilikom pucanja veze
- Mogucnost za force reconnect-om ukoliko je upkla veza
- Globalni exception/error handling
- Podesiv sistem alarmiranja koji se edituje realtime
- Istorija poruka filtrirana po vremenu
- Password hashing za bolju bezbednost
- Mogucnost promene lozinke
- Opcija da simulator cita izlaznu vrednost iz konfiguracije
- Opcija da admin menja izlaznu vrednost uredjaja (simulatora)
***
## 📀 - **Prerequisites** - 📀
- .NET SDK 8
- Visual Studio (Code) / Rider
- Docker
- MQTT Broker
- Swagger (za testiranje)
- MariaDB i Redis
***
## 🧑‍💻 - **Setup Instrukcije** - 🧑‍💻
- Neophodno je prethodno kloniranje git repozitorijuma
- U folderu **'MQTTRestApi'** neophodno je popuniti .env fajl (primer u .env.example)
- Unutar foldera **'MQTTRestApi'** pritisnuti desni klik > **Open in Terminal**
- Ukucati **docker compose up -d --build**
- Ukoliko nema gresaka, API je spreman za upotrebu
***
## 🌐 - **API Endpoints** - 🌐
### Alarm ENDPOINTS:
- **(POST)api/alarms/create**
  - Input parametri:
    - topicId (int32)
    - alarmType (0, 1, 2)
    - lowerLimit (int32)
    - upperLimit (int32)
  - Kreira alarm
- **(POST)api/alarms/delete**
  - Input parametri:
    - alarmId (int32)
  - Brise alarm iz baze podataka
- **(GET)api/alarms/all**
  - Input parametri:
    - Nema input parametre
  - Vraca listu svih kreiranih alarma
- **(GET)api/alarms/alarmed-messages**
  - Input parametri:
    - Nema input parametre
  - Vraca listu svih alarmiranih poruka
### Log ENDPOINTS:
- **(GET)api/logs/all-logs**
  - Input parametri:
    - Nema input parametre
  - Vraca listu svih logova
### User ENDPOINTS:
- **(POST)api/users/login**
    - Input parametri:
        - LoginDto (username, password)
    - Vrsi prijavu korisnika na nalog
- **(POST)api/users/register**
    - Input parametri:
        - RegisterDto (username, password, userType)
    - Vrsi registraciju korisnika u sistem
- **(POST)api/users/change-password**
  - Input parametri:
    - ChangePasswordDto (oldPassword, newPassword)
  - Vrsi promenu sifre korisnika
- **(POST)api/users/change-role**
  - Input parametri:
    - userId (int32)
    - role (0, 1)
  - Vrsi promenu role-a korisnika
- **(GET)api/users/list**
    - Input parametri:
        - Nema input parametre
    - Vraca listu korisnika registrovanih u sistem
### MQTT Admin ENDPOINTS:
- **(POST)api/mqtt/subscribe**
   - Input parametri:
      - Topic (string)
   - Vrsi pretplatu backend-a na neki topic
   - Podrzava single i multi level wildcards
- **(GET)api/mqtt/failed-subscriptions**
   - Input parametri:
      - Nema input parametre
   - Vraca listu svih subscriptiona na koje broker nije uspeo da se prijavi
- **(POST)api/mqtt/resubscribe-to-failed**
   - Input parametri:
      - Nema input parametre
   - Sluzi za pokusaj ponovne prijave na subscribere na koje broker nije uspeo da se prijavi
- **(GET)api/mqtt/stats**
   - Input parametri:
      - Nema input parametre
   - Vraca broj koliko puta je bila poslata poruka na svaki od topic-a
- **(GET)api/mqtt/stats/{topic}**
    - Input parametri:
        - Topic (string)
    - Vraca broj koliko puta je bila poslata poruka na odredjen topic
- **(POST)api/mqtt/force-reconnect-to-server**
    - Input parametri:
        - Nema input parametre
    - Vraca broj koliko puta je bila poslata poruka na svaki od topic-a
### User MQTT ENDPOINTS:
- **(GET)api/mqtt/available-topics**
  - Input parametri:
    - Nema input parametre
  - Vraca listu topics-a na koje je broker pretplacen
- **(POST)api/mqtt/user-subscribe**
    - Input parametri:
        - SubscribeRequestDto (topicId, topic)
    - Vrsi pretplatu korisnika na topic
- **(POST)api/mqtt/user-unsubscribe/{topicId}**
    - Input parametri:
        - topicId (int32)
    - Uklanja korisnicku pretplatu sa topic-a
- **(GET)api/mqtt/my-subscriptions**
    - Input parametri:
        - Nema input parametre
    - Vraca listu svih topica na koje je korisnik pretplacen
- **(GET)api/mqtt/messages**
    - Input parametri:
        - Nema input parametre
    - Vraca listu svih poruka za topic-e na koje je korisnik pretplacen
- **(GET)api/mqtt/all-messages**
  - Input parametri:
    - Nema input parametre
  - Vraca listu svih poruka koje su ikada stigle
- **(GET)api/mqtt/messages-filtered**
  - Input parametri:
    - topic (string)
    - minutes (int)
  - Vraca listu svih poruka za topic u poslednjih n minuta
- **(GET)api/mqtt/suggest-device-value-change**
  - Input parametri:
    - topic (string)
    - value (float)
  - Govori uredjaju da azurira vrednost na tom topicu
***
## ⌨️ - **Primeri curl komandi** - ⌨️
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
***
## 🎛️ - **Testing instrukcije** - 🎛️
- Za pokretanje Unit testova neophodno je:
- Otvoriti folder **'MqttRestApi.Tests'**
- Pritisnuti desni klik > **Open in Terminal**
- ukucati:
```bash 
    dotnet test
```
***
## 📃 - **Licenca** - 📃
- Trenutno ne postoji definisana licenca za ovaj proizvod
***
## 📜 - **Ranije izmene i azuriranja** - 📜️
### **-= Verzija 0.26.10.12.1 =-**

- **User authentication:**
  - Dodata **autentifikacija korisnika** putem korisnickog **imena** i **lozinke**
  - Uspesan login/register generise **JWT token** za korisnika
- **Subscriptions:**
  - Dodata opcija da se korisnik **licno pretplati** na neki topic iz **liste dostupnih topica**
  - **Lista dostupnih topica** predstavlja **listu topica** na koje je backend prijavljen
  - Korisnik takodje moze da se **odjavi sa topic-a**
  - Dodato da korisnik vidi poruke samo za topic-e na koje je **prijavljen**
  - Sve **MQTT rute** su zakljucane za **neprijavljene korisnike (goste)**
  - Neuspesne pretplate se cuvaju u **listi** i admin moze **ponovo da pokusa da pretplati backend** na njih
  - Dodat inkrementalni tajmer za ponovno povezivanje prilikom pucanja veze sa brokerom, vreme se podesava u **appsettings.json** fajlu
  - Dodato da **admin** moze **rucno da pokusa reconnect** u slucaju pucanja konekcije (nezavisno od tajmera)
- **Error Handling:**
  - Dodat **global exception/error handling** unutar servisa
- **Controllers:**
  - **api/mqtt/resubscribe-to-failed** promenjen sa **Get** na **Post**
  - **api/mqtt/force-reconnect-to-server** promenjen sa **Get** na **Post**
  - **api/users/login** promenjen sa **Get** na **Post**
  - **api/users/register** promenjen sa **Get** na **Post**
  - **api/users/list** promenjen sa **Post** na **Get**
- **Services:**
  - Funkcija koju poziva **ReconnectToAll()** izmestena u privatnu funkciju **ConnectOnBrokerAsync()**
  - Izmenjeno da **SubscribeAsync()** sluzi samo za prijavu backenda na topic preko rute
  - Popravljen **bug** pogresnog redosleda prilikom **subscribe-a**
- **Database:**
  - Tabela **'Subscriptions'** zamenjena tabelom **'Topics'**
  - Nova vezna tabela **'UserSubscriptions'** koja povezuje Korisnika (**UserId**) i Topic (**TopicID**)
  - Dodat **unique index** za tabele kako bi se izbegao race condition u **AnyAsync**
- **Tests:**
  - Unit testovi **privremeno obrisani** zbog konstantnog azuriranja servisa
- **README.md**
  - Azurirana **features lista**
  - Azurirana **lista ruta**
  - Dodat **change log** za verzije
  - Sredjeno **formatiranje teksta**
***
### **-= Verzija 0.26.10.9.1 =-**
- **Simulator:**
  - Dodat novi **projekat** unutar **solution-a** koji sluzi za **simuliranje uredjaja**
  - Dodate **opcije za simuliranje** u vidu:
    - Minimalne vrednosti
    - Maksimalne vrednosti
    - Intervala slanja poruka
- **RestApi:**
  - Iz **RestApi** projekta izbacena mogucnost **slanja poruke (publish)**