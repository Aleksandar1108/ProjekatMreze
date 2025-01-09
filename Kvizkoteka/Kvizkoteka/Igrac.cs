using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kvizkoteka
{
    public class Igrac
    {
        public int Id { get; set; }
        public string ImeNadimak { get; set; }
        public int[] Rezultat { get; set; }
        public bool UlozenKvisko { get; set; } = false;
    }
}
