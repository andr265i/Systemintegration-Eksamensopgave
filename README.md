# Systemintegration Eksamensopgave

# Eaat - Distribueret Food Delivery Platform

Dette projekt er lavet til systemintegration eksamen på 4.semester på datamatikeruddannelsen. 
System er et distribueret mikroservice-system til madbestilling og levering (Eaat). Systemet demonstrerer asynkron kommunikation, event-dreven arkitektur og distribuerede data-mønstre.

## Arkitektur
Systemet består af tre uafhængige mikroservices, der hver har sin egen database (Share-Nothing Architecture). De kommunikerer udelukkende asynkront via RabbitMQ:
* **OrderService:** Håndterer kundens bestillinger.
* **RestaurantService:** Multi-tenant service til restauranter, hvor de kan se og acceptere egne ordrer.
* **CourierService:** Håndterer udbud af leveringsopgaver til bude baseret på geografi (ZipCode).

## Forudsætninger for kørsel
For at køre systemet lokalt er det designet til at være så nemt som muligt:

1. **Databaseserver er ikke påkrævet:** Projektet anvender **SQLite**. Databaserne oprettes automatisk som lokale `.db` filer i projektmapperne.
2. **RabbitMQ:** Der kræves en kørende RabbitMQ-instans.

**Start RabbitMQ via Docker:**
For at undgå manuel installation og For at køre præcis samme miljø som projektet er udviklet i, skal RabbitMQ startes med denne Docker-kommando i terminalen:
`docker run -d --name rabbitmq -p 5672:5672 -p 8080:15672 rabbitmq:4-management`

## Sådan startes projektet
1. Klon repository.
2. Sørg for at RabbitMQ-containeren kører via ovenstående kommando.
3. Start de tre API'er (i Visual Studio (Set Startup Projects -> Multiple startup projects)).
4. Brug Scalar UI til at interagere med de forskellige services.
