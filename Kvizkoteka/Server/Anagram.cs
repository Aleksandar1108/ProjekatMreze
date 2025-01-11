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
            
            return RecOdKojeSePraviAnagram.Replace(" ", "").Length;
        }
    }
}
