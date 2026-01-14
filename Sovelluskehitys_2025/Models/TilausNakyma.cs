using System;
using System.Collections.Generic;

namespace Sovelluskehitys_2025.Models
{
    // Näkymämallit pää- ja rivinäkymään
    public class TilausNakyma
    {
        // Tilauksen otsikkokentät päätaulussa
        public long Id { get; set; }
        public string AsiakasNimi { get; set; } = "";
        public string Osoite { get; set; } = "";
        public DateTime? TilausPvm { get; set; }
        public DateTime? ToimitusPvm { get; set; }
        public bool Toimitettu { get; set; }
        public decimal Yhteensa { get; set; }
        // Rivien tiedot alataulussa.
        public List<TilausRiviNakyma> Rivit { get; set; } = new List<TilausRiviNakyma>();
    }

    public class TilausRiviNakyma
    {
        // Tilausrivin kentät yksittäisen tilauksen alla
        public long RiviId { get; set; }
        public long TuoteId { get; set; }
        public string TuoteNimi { get; set; } = "";
        public int Maara { get; set; }
        public decimal Rivihinta { get; set; }
    }
}
