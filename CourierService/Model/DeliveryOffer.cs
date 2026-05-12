using System.ComponentModel.DataAnnotations;

namespace CourierService.Model
{
    public class DeliveryOffer
    {
        // Vi bruger ordrens ID som ID her, så vi altid kan kæde dem sammen
        public Guid Id { get; set; }

        // For at sikre os, at kun ét bud kan tage opgaven, bruger vi en simpel string-status med en ConcurrencyCheck.
        //ConcurrencyCheck betyder, at hvis to bude prøver at ændre status på samme tid, vil kun den første lykkes, og den anden vil få en fejl, som vi kan håndtere i controlleren.
        [ConcurrencyCheck]
        public string Status { get; set; } = "Free"; // "Free" eller "Taken"
        public Guid? AssignedCourierId { get; set; } // Hvem vandt ræset? (Null indtil da)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
