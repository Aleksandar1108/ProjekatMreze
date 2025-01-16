using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class PitanjaIOdgovori
    {
        public string TekucePitanje { get; private set; } // Trenutno pitanje
        public bool TacanOdgovor { get; private set; } // Da li je tačan odgovor na trenutno pitanje
        public Dictionary<string, bool> SvaPitanja { get; private set; } // Sva pitanja i odgovori

        private Random random; // Generator slučajnih brojeva za nasumičan izbor pitanja

        public PitanjaIOdgovori()
        {
            SvaPitanja = new Dictionary<string, bool>();
            random = new Random();
        }

        // Metoda za popunjavanje rečnika sa pitanjima i tačnim odgovorima
        public void UcitajPitanja()
        {
            SvaPitanja.Add("Čovek ima tri bubrega.", false);
            SvaPitanja.Add("Voda ključa na 100 stepeni Celzijusa.", true);
            SvaPitanja.Add("Sunce je planeta.", false);
            SvaPitanja.Add("Ptice mogu leteti.", true);
            SvaPitanja.Add("Postoji život na Marsu.", false);
            SvaPitanja.Add("Najveći okean na svetu je Tihi okean.", true);
            SvaPitanja.Add("Krave lete.", false);
            SvaPitanja.Add("Čovek ima 206 kostiju.", true);
            SvaPitanja.Add("Zemlja ima dva Meseca.", false);
            SvaPitanja.Add("Led je lakši od vode.", true);
        }

        public bool PostaviPitanje(List<bool> prethodniOdgovori)
        {

            if (SvaPitanja.Count == 0) return false; // Nema više pitanja

            // Sprečavanje više od tri ista odgovora u sekvenci
            bool dozvoliTacan = prethodniOdgovori.Count < 3 || prethodniOdgovori.Skip(Math.Max(0, prethodniOdgovori.Count - 3)).Any(o => !o);
            bool dozvoliNetacan = prethodniOdgovori.Count < 3 || prethodniOdgovori.Skip(Math.Max(0, prethodniOdgovori.Count - 3)).Any(o => o);

            // Filtriraj pitanja prema pravilima
            var validnaPitanja = SvaPitanja.Where(p =>
                (dozvoliTacan && p.Value) ||
                (dozvoliNetacan && !p.Value)
            ).ToList();

            if (validnaPitanja.Count == 0) return false; // Nema validnih pitanja

            // Izbor nasumičnog pitanja iz filtrirane liste
            var odabrano = validnaPitanja[random.Next(validnaPitanja.Count)];
            TekucePitanje = odabrano.Key;
            TacanOdgovor = odabrano.Value;

            // Uklanjanje pitanja koje je postavljeno
            SvaPitanja.Remove(odabrano.Key);

            return true;
        }


        // Metoda za proveru odgovora
        public bool ProveriOdgovor(string odgovor)
        {
            // Proveravamo da li je odgovor tačan na osnovu definicije TacanOdgovor
            return (odgovor.ToUpper() == "A" && TacanOdgovor) || (odgovor.ToUpper() == "B" && !TacanOdgovor);
        }

    }
}
