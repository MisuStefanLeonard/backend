using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using E_Commerce_BackEnd.Models.Enums;
using E_Commerce_BackEnd.Models.OrderRelatedModels;

namespace E_Commerce_BackEnd.Models.UserRelatedModels
{
    public class Adrese 
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdAdresa { get; init; }
        
        [StringLength(20)]
        public string Alias { get; init; }
        public TipAdrese TipAdresa { get; init; }
        
        [StringLength(5)]
        public string? Bloc { get; init; }
        
        [StringLength(5)]
        public string? NrBloc { get; init; }
        
        [StringLength(30)]
        public string Strada { get; init; }
        
        [StringLength(3)]
        public string NrStrada { get; init; }
        
        public bool IsDeleted { get; set; }
        
        public int IdLocatie { get; init; }
        public  Locatii Locatie { get;  }
        
        public int? IdCont { get; init; } // id cont
        public  Conturi? Cont { get;  } 
        public ICollection<DetaliiFactura>? DetaliiFacturi { get;}
        
        public Adrese()
        {
            
        }

        public Adrese(string alias,TipAdrese tipAdresa, string? bloc, string? nrBloc, 
            string strada, string nrStrada, int idLocatie, 
            Locatii locatie, int idCont, Conturi cont , ICollection<DetaliiFactura>? detaliiFactura, bool isDeleted)
        {
            TipAdresa = tipAdresa;
            Bloc = bloc;
            NrBloc = nrBloc;
            Strada = strada;
            NrStrada = nrStrada;
            IdLocatie = idLocatie;
            Locatie = locatie;
            IdCont = idCont;
            Cont = cont;
            IsDeleted = isDeleted;
            Alias = alias;
            DetaliiFacturi = detaliiFactura == null ? [] : new HashSet<DetaliiFactura>(detaliiFactura);
            
        }
        
    }
}