using System.Collections.Generic;

namespace Sovelluskehitys_2025.Models
{
    public class TilausNakyma
    {
        public long Id { get; set; }
        public string AsiakasNimi { get; set; } = "";
        public string Osoite { get; set; } = "";
        public bool Toimitettu { get; set; }
        public decimal Yhteensa { get; set; }
        public List<TilausRiviNakyma> Rivit { get; set; } = new List<TilausRiviNakyma>();
    }

    public class TilausRiviNakyma
    {
        public long RiviId { get; set; }
        public long TuoteId { get; set; }
        public string TuoteNimi { get; set; } = "";
        public int Maara { get; set; }
        public decimal Rivihinta { get; set; }
    }
}
