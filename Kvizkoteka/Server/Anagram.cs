using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Anagram
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
            // ISPRAVKA: Proverava da li je predloženi odgovor IDENTIČAN originalnoj reči
            // Uklanjanje razmaka i pretvaranje u mala slova za poređenje
            string originalClean = RecOdKojeSePraviAnagram.Replace(" ", "").ToLower();
            string predlozenClean = PredloženAnagram.Replace(" ", "").ToLower();

            // Direktno poređenje - mora biti identično
            return originalClean == predlozenClean;
        }

        public int IzracunajPoene()
        {
            // ISPRAVKA: Koristi novu logiku iz ProveriAnagram metode
            if (ProveriAnagram())
            {
                // Ako je tačan, dodeli bodove na osnovu broja slova u originalnoj reči bez razmaka
                string originalNoSpaces = RecOdKojeSePraviAnagram.Replace(" ", "").ToLower();
                return originalNoSpaces.Length;
            }

            // Ako nije tačan, nema bodova
            return 0;
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