using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public  class Anagram
    {
        public string RecOdKojeSePraviAnagram { get; set; } // Originalna reč
        public string PredloženAnagram { get; set; } // Predloženi anagram od strane igrača

        // Metoda za učitavanje reči iz tekstualne datoteke
        public void UcitajRec(string fileName)
        {
            try
            {
                var lines = File.ReadAllLines(fileName);
                var random = new Random();
                // Učitavamo nasumičnu liniju iz fajla
                RecOdKojeSePraviAnagram = lines[random.Next(lines.Length)];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška prilikom učitavanja fajla: {ex.Message}");
            }
        }

        // Metoda za generisanje pomešanih slova iz originalne reči
        public string GenerisiAnagram()
        {
            var random = new Random();
            return new string(RecOdKojeSePraviAnagram.ToCharArray()
                .OrderBy(c => random.Next())
                .ToArray());
        }

        // Metoda koja proverava da li je predloženi anagram validan
        public bool ProveriAnagram()
        {
            // Sortiramo originalnu reč i predloženi anagram
            var originalSorted = string.Concat(RecOdKojeSePraviAnagram.OrderBy(c => c));
            var predlozenSorted = string.Concat(PredloženAnagram.OrderBy(c => c));

            // Ako su isti, anagram je tačan
            return originalSorted == predlozenSorted;
        }

        // Metoda za računanje broja poena (broj slova u originalnom tekstu)
        public int IzracunajPoene()
        {
            // Broji ukupno slova u originalnom tekstu (ignorise praznine)
            return RecOdKojeSePraviAnagram.Replace(" ", "").Length;
        }
    }
}
