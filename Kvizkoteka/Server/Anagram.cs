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
        public string RecOdKojeSePraviAnagram { get; set; } 
        public string PredloženAnagram { get; set; } 

        
        public void UcitajRec(string fileName)
        {
            try
            {
                var lines = File.ReadAllLines(fileName);
                var random = new Random();
                
                RecOdKojeSePraviAnagram = lines[random.Next(lines.Length)];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška prilikom učitavanja fajla: {ex.Message}");
            }
        }

        
        public string GenerisiAnagram()
        {
            // Podela originalne reci na reci
            string[] words = RecOdKojeSePraviAnagram.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Random rand = new Random();

            List<string> scrambledWords = new List<string>();

            foreach (var word in words)
            {
                // Pretvaramo svaku rec u karaktere i mesamo te karaktere
                char[] characters = word.ToCharArray();
                for (int i = 0; i < characters.Length; i++)
                {
                    int j = rand.Next(i, characters.Length); // Random indeks za zamenu
                    char temp = characters[i];
                    characters[i] = characters[j];
                    characters[j] = temp;
                }

              
                scrambledWords.Add(new string(characters));
            }

            // Spajamo pomesane reci kao u jednu recenicu
            return string.Join(" ", scrambledWords);
        }

        
        public bool ProveriAnagram()
        {
            
            var originalSorted = string.Concat(RecOdKojeSePraviAnagram.OrderBy(c => c));
            var predlozenSorted = string.Concat(PredloženAnagram.OrderBy(c => c));

            
            return originalSorted == predlozenSorted;
        }

        
        public int IzracunajPoene()
        {

            // return RecOdKojeSePraviAnagram.Replace(" ", "").Length;

            var originalWords = RecOdKojeSePraviAnagram.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var predlozeniWords = PredloženAnagram.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            int totalPoints = 0;

            // Prolazak kroz sve originalne reči
            for (int i = 0; i < originalWords.Length; i++)
            {
                // Ako nema dovoljno predloženih reči, prekidamo dalju obradu
                if (i >= predlozeniWords.Length) break;

                var originalWord = originalWords[i];
                var predlozenaWord = predlozeniWords[i];

                // Proveri da li je predložena reč tačan anagram originalne reči
                if (IsWordAnagram(originalWord, predlozenaWord))
                {
                    // Ako je tačna, dodaj poene jednako broju slova
                    totalPoints += originalWord.Length;
                }
                // Inače, ne dodajemo poene (automatski ostaje 0)
            }

            return totalPoints;
        }

        private bool IsWordAnagram(string originalWord, string predlozenaWord)
        {
            // Ako dužine nisu iste, automatski nisu anagrami
            if (originalWord.Length != predlozenaWord.Length) return false;

            // Kreiraj frekvencijski brojnik za karaktere u originalnoj reči
            var charCount = new Dictionary<char, int>();

            foreach (var c in originalWord)
            {
                if (charCount.ContainsKey(c))
                    charCount[c]++;
                else
                    charCount[c] = 1;
            }

            // Proveri karaktere predložene reči protiv originalne
            foreach (var c in predlozenaWord)
            {
                if (!charCount.ContainsKey(c) || charCount[c] == 0)
                    return false; // Slovo ne postoji ili je višak
                charCount[c]--;
            }

            // Ako svi karakteri odgovaraju, reči su anagrami
            return true;
        }
    }
}
