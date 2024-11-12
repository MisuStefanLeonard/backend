using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using E_Commerce_BackEnd.Models.Enums;
using E_Commerce_BackEnd.Models.OrderRelatedModels;

namespace E_Commerce_BackEnd.Models.UserRelatedModels
{
    public class Adrese 
    {
        public int IdAdresa { get; init; }

        [StringLength(20)] 
        public string Alias { get; init; } = null!;
        public TipAdrese TipAdresa { get; init; }
        
        [StringLength(5)]
        public string? Bloc { get; init; }
        
        [StringLength(5)]
        public string? NrBloc { get; init; }

        [StringLength(30)] 
        public string Strada { get; init; } = null!;

        [StringLength(3)] 
        public string NrStrada { get; init; } = null!;
        
        public bool IsDeleted { get; set; }
        
        public int IdLocatie { get; set; }
        public Locatii Locatie { get; init; } = null!;
        
        public int IdCont { get; init; } // id cont
        public Conturi Cont { get; init; } = null!;
        
        public int? IdDetaliuFactura { get; set; }
        public DetaliiFactura? DetaliuFactura { get; init; }

        public ICollection<Comenzi>? AdreseLivrarePeComanda { get; } = new HashSet<Comenzi>();
        public ICollection<Comenzi>? AdreseFacturarePeComanda { get; } = new HashSet<Comenzi>();

    }
}