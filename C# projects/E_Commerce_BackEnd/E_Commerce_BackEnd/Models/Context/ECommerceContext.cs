using E_Commerce_BackEnd.Models.OrderRelatedModels;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Models.ProductVouchersModels;
using E_Commerce_BackEnd.Models.UserRelatedModels;
using Microsoft.EntityFrameworkCore.Metadata;

namespace E_Commerce_BackEnd.Models.Context;
using Microsoft.EntityFrameworkCore;
public class ECommerceContext : DbContext
{
    
    public ECommerceContext(DbContextOptions<ECommerceContext> options) : base(options) { }
    
    public ECommerceContext(){}

    #region DbSet

    /// <summary>
    /// User related tables !!!
    /// </summary>
    public  DbSet<Locatii> DbLocatii { get; set; }
    public  DbSet<Adrese> DbAdrese { get; set; }
    public  DbSet<Conturi> DbConturi { get; set; }

    /// <summary>
    /// Order and addresses related tables !!!
    /// </summary>
    public  DbSet<DetaliiFactura> DbDetaliiFactura { get; set; }
    public  DbSet<Comenzi> DbComenzi { get; set; }
    public  DbSet<ProduseCuComenzi> DbProduseCuComenzi { get; set; }

    /// <summary>
    /// Products related tables !!!
    /// </summary>
    public  DbSet<Dimensiuni> DbDimensiuni { get; set; }
    public  DbSet<Producatori> DbProducatori { get; set; }
    public  DbSet<CodCulori> DbCodCulori { get; set; }
    public  DbSet<Culori> DbCulori { get; set; }
    public  DbSet<Produse> DbProduse { get; set; }
    public  DbSet<Seturi> DbSeturi { get; set; }
    public  DbSet<Imagini> DbImagini { get; set; }
    public  DbSet<Manopere> DbManopere { get; set; }
    public  DbSet<TipuriProduse> DbTipuriProduse { get; set; }
    public  DbSet<TipuriPeProduse> DbTipuriPeProduse { get; set; }
    public  DbSet<AsociereSeturi> DbAsociereSeturi { get; set; }
    public  DbSet<ProduseCuCulori> DbProduseCuCulori { get; set; }
    public  DbSet<ProduseCuDimensiuni> DbProduseCuDimensiuni { get; set; }

    /// <summary>
    /// Vouchers and products related tables
    /// </summary>
    public  DbSet<Vouchere> DbVouchere { get; set; }
    public  DbSet<ProduseCuVouchere> DbProduseCuVouchere { get; set; }

    #endregion
   
    
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Locatii>(entity =>
        {
            entity.ToTable("locatii");
            entity.HasKey(e => e.IdLocatie);
            
            entity.Property(e => e.IdLocatie)
                .HasColumnType("integer")
                .HasColumnName("id_locatie")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            entity.Property(e => e.Oras)
                .HasColumnType("varchar")
                .HasColumnName("oras")
                .HasMaxLength(20);
            
            entity.Property(e => e.CodPostal)
                .HasColumnType("varchar")
                .HasColumnName("cod_postal")
                .HasMaxLength(15);
            
            entity.Property(e => e.Judet)
                .HasColumnType("varchar")
                .HasColumnName("judet")
                .HasMaxLength(15);

            entity.HasMany(e => e.AdreseLocatii)
                .WithOne(e => e.Locatie)
                .HasForeignKey(e => e.IdLocatie)
                .HasConstraintName("FK_Locatii")
                .IsRequired();
        });
        
        modelBuilder.Entity<Conturi>(entity =>
        {
            entity.ToTable("conturi");
            entity.HasKey(e => e.IdCont);

            entity.Property(e => e.IdCont)
                .HasColumnName("id_cont")
                .HasColumnType("integer")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            entity.Property(e => e.Nume)
                .HasMaxLength(10)
                .HasColumnName("nume")
                .HasColumnType("varchar");
            
            entity.Property(e => e.Prenume)
                .HasMaxLength(20)
                .HasColumnName("prenume")
                .HasColumnType("varchar");
            
            entity.Property(e => e.Gen)
                .HasColumnName("gen")
                .HasColumnType("tinyint");

            entity.Property(e => e.NrTelefon)
                .HasColumnName("nr_telefon")
                .HasColumnType("varchar")
                .HasMaxLength(10);

            entity.Property(e => e.Username)
                .HasColumnName("username")
                .HasColumnType("varchar")
                .HasMaxLength(15)
                .IsRequired();
            
            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasColumnType("varchar")
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.Parola)
                .HasColumnName("parola")
                .HasColumnType("varchar")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(e => e.DataCreare)
                .HasColumnName("data_creare")
                .HasColumnType("datetime")
                .HasDefaultValueSql("NOW()");
            
            entity.Property(e => e.CodActivare)
                .HasColumnName("cod_activare")
                .HasColumnType("varchar")
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.Verificat)
                .HasColumnName("verificat")
                .HasColumnType("tinyint")
                .IsRequired();
            
            entity.Property(e => e.IsGuest)
                .HasColumnName("guest")
                .HasColumnType("tinyint")
                .HasDefaultValue(false)
                .IsRequired();

            
            entity.Property(e => e.Rol)
                .HasColumnName("rol")
                .HasColumnType("varchar")
                .HasMaxLength(15)
                .IsRequired();

            entity.Property(e => e.OraLinkConfirmare)
                .HasColumnName("ora_link")
                .HasColumnType("datetime")
                .HasDefaultValueSql("NOW()");

            entity.HasMany(e => e.AdreseConturi)
                .WithOne(e => e.Cont)
                .HasForeignKey(e => e.IdCont)
                .HasConstraintName("FK_Conturi")
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        modelBuilder.Entity<Adrese>(entity =>
        {
            entity.ToTable("adrese");
            entity.HasKey(e => e.IdAdresa);

            entity.Property(e => e.IdAdresa)
                .HasColumnName("id_adresa")
                .HasColumnType("integer")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            entity.Property(e => e.Alias)
                .HasColumnName("alias")
                .HasColumnType("varchar")
                .HasMaxLength(20);
            
            entity.Property(e => e.TipAdresa)
                .HasColumnName("tip_adresa")
                .HasConversion<string>()
                .IsRequired();
            
            
            entity.Property(e => e.Bloc)
                .HasColumnName("bloc")
                .HasColumnType("varchar")
                .HasMaxLength(10);
            
            entity.Property(e => e.NrBloc)
                .HasColumnName("nr_bloc")
                .HasColumnType("varchar")
                .HasMaxLength(7);
            
            entity.Property(e => e.Strada)
                .HasColumnName("strada")
                .HasColumnType("varchar")
                .HasMaxLength(30)
                .IsRequired();
            
            entity.Property(e => e.NrStrada)
                .HasColumnName("nr_strada")
                .HasColumnType("varchar")
                .HasMaxLength(5)
                .IsRequired();

            entity.Property(e => e.IdCont)
                .HasColumnName("id_cont")
                .IsRequired();
            
            entity.Property(e => e.IdLocatie)
                .HasColumnName("id_locatie")
                .IsRequired();

            entity.Property(e => e.IsDeleted)
                .HasColumnName("isDeleted")
                .HasColumnType("tinyint")
                .HasDefaultValue(false)
                .IsRequired();
            
            entity.HasMany(a => a.DetaliiFacturi)
                .WithOne(df => df.Adrese)
                .HasForeignKey(df => df.IdAdresa)
                .HasConstraintName("FK_Adrese")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

        });
        
        modelBuilder.Entity<DetaliiFactura>(entity =>
        {
            entity.ToTable("detalii_factura");
            entity.HasKey(e => e.IdDetaliu);

            entity.Property(e => e.IdDetaliu)
                .HasColumnName("id_detaliu")
                .HasColumnType("integer")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            entity.Property(e => e.Cif)
                .HasColumnType("varchar")
                .HasColumnName("CIF")
                .HasMaxLength(12);

            entity.Property(e => e.NumeFirma)
                .HasColumnType("varchar")
                .HasColumnName("nume_firma")
                .HasMaxLength(50);
            
            
            entity.Property(e => e.IdAdresa)
                .HasColumnType("integer")
                .HasColumnName("id_adresa")
                .IsRequired();

            entity.HasMany(df => df.DComenzi)
                .WithOne(com => com.CDetaliiFactura)
                .HasForeignKey(com => com.IdDetaliu)
                .HasConstraintName("FK_DetaliiFactura")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        
        modelBuilder.Entity<Comenzi>(entity =>
        {
            entity.ToTable("comenzi");
            entity.HasKey(e => e.IdComanda);

            entity.Property(e => e.IdComanda)
                .HasColumnType("integer")
                .HasColumnName("id_comanda")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

         
            entity.Property(e => e.IdDetaliu)
                .HasColumnName("id_detaliu")
                .HasColumnType("integer")
                .IsRequired();
           
            entity.Property(e => e.DataEmitereComanda)
                .HasColumnName("data_emitere_comanda")
                .HasColumnType("datetime")
                .HasDefaultValueSql("NOW()")
                .IsRequired();

            entity.Property(e => e.StatusComanda)
                .HasColumnName("status_comanda")
                .HasConversion<string>()
                .IsRequired();
            
            entity.Property(e => e.TipPlata)
                .HasColumnName("tip_plata")
                .HasConversion<string>()
                .IsRequired();
            
        });

        modelBuilder.Entity<ProduseCuComenzi>(entity =>
        {
            entity.ToTable("produse_cu_comenzi");
            entity.HasKey(e => e.IdProduseCuComenzi);
            
            entity.Property(e => e.IdProduseCuComenzi)
                .HasColumnName("id_produse_cu_comenzi")
                .HasColumnType("integer")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            entity.Property(e => e.IdComanda)
                .HasColumnName("id_comanda")
                .HasColumnType("integer")
                .IsRequired();

            entity.Property(e => e.NrBucati)
                .HasColumnName("nr_buc")
                .HasColumnType("integer")
                .IsRequired();
            
            entity.Property(e => e.IdCuloare)
                .HasColumnName("id_culoare")
                .HasColumnType("integer")
                .IsRequired();
            
            entity.Property(e => e.IdDimensiune)
                .HasColumnName("id_dimensiune")
                .HasColumnType("integer")
                .IsRequired();

            entity.Property(e => e.IdManopera)
                .HasColumnName("id_manopera")
                .HasColumnType("integer");
               
            
            entity.Property(e => e.IdProdus)
                .HasColumnName("id_produs")
                .HasColumnType("integer")
                .IsRequired();
            
            entity.HasOne(pc => pc.Produs)
                .WithMany(p => p.ComenziProduse)
                .HasForeignKey(pc => pc.IdProdus)
                .HasConstraintName("FK_Produse_PC")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            entity.HasOne(pc => pc.Comanda)
                .WithMany(c => c.PcComenzi)
                .HasForeignKey(pc => pc.IdComanda)
                .HasConstraintName("FK_Comenzi_PC")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });
        
        modelBuilder.Entity<Dimensiuni>(entity =>
        {
         
            entity.ToTable("dimensiuni");
            entity.HasKey(e => e.IdDimensiune);

            entity.Property(e => e.IdDimensiune)
                .HasColumnType("integer")
                .HasColumnName("id_dimensiune")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            entity.Property(e => e.Lungime)
                .HasColumnType("varchar")
                .HasColumnName("lungime")
                .HasMaxLength(4)
                .IsRequired();
            
            entity.Property(e => e.RecomandarePat)
                .HasColumnType("varchar")
                .HasColumnName("recomandare_pat")
                .HasMaxLength(15);
            
            entity.Property(e => e.Latime)
                .HasColumnType("varchar")
                .HasColumnName("latime")
                .HasMaxLength(4)
                .IsRequired();
            
        });
        
        modelBuilder.Entity<Producatori>(entity =>
        {
            entity.ToTable("producatori");
            entity.HasKey(e => e.IdProducator);

            entity.Property(e => e.IdProducator)
                .HasColumnType("integer")
                .HasColumnName("id_producator")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            entity.Property(e => e.NumeProducator)
                .HasColumnType("varchar")
                .HasColumnName("nume_producator")
                .HasMaxLength(30);

        });

        modelBuilder.Entity<CodCulori>(entity =>
        {
            entity.ToTable("cod_culori");
            entity.HasKey(e => e.IdCodCuloare);

            entity.Property(e => e.IdCodCuloare)
                .HasColumnType("integer")
                .HasColumnName("id_cod_culoare")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            entity.Property(e => e.CodCuloare)
                .HasColumnName("cod_culoare")
                .HasColumnType("varchar")
                .HasMaxLength(5);

            entity.HasMany(cc => cc.CoduriCulori)
                .WithOne(c => c.CodCuloare)
                .HasForeignKey(c => c.IdCodCuloare)
                .HasConstraintName("FK_CodCulori");

        });
        
        
        modelBuilder.Entity<Culori>(entity =>
        {
            entity.ToTable("culori");
            entity.HasKey(e => e.IdCuloare);

            entity.Property(e => e.IdCuloare)
                .HasColumnType("integer")
                .HasColumnName("id_culoare")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            entity.Property(e => e.NumeCuloare)
                .HasColumnName("nume_culoare")
                .HasColumnType("varchar")
                .HasMaxLength(20)
                .IsRequired();
            
            entity.Property(e => e.IdCodCuloare)
                .HasColumnName("id_cod_culoare")
                .HasColumnType("integer")
                .IsRequired();
            
        });

        modelBuilder.Entity<ProduseCuCulori>(entity =>
        {
            entity.ToTable("culori_produse");
            entity.HasKey(e => e.IdProdusCuCuloare);
            
            
            entity.Property(e => e.IdProdusCuCuloare)
                .HasColumnType("integer")
                .HasColumnName("id_produs_culoare")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            entity.Property(e => e.IdProdus)
                .HasColumnName("id_produs")
                .HasColumnType("integer")
                .IsRequired();

            entity.Property(e => e.IdCuloare)
                .HasColumnType("integer")
                .HasColumnName("id_culoare")
                .IsRequired();


            entity.HasOne(pc => pc.Culoare)
                .WithMany(c => c.CProduseCuCulori)
                .HasForeignKey(pc => pc.IdCuloare)
                .HasConstraintName("FK_Culori_ProduseCuCulori");

            entity.HasOne(pc => pc.Produse)
                .WithMany(c => c.PProduseCuCulori)
                .HasForeignKey(pc => pc.IdProdus)
                .HasConstraintName("FK_Produse_ProduseCuCulori")
                .IsRequired();
            
            entity.HasMany(pc => pc.ImagProduseCuCulori)
                .WithOne(i => i.ProdusCuCuloare)
                .HasForeignKey(i => i.IdProdusCuCuloare)
                .HasConstraintName("FK_ProduseCuCuloare_Imagini")
                .IsRequired();
            
        });

        modelBuilder.Entity<Produse>(entity =>
        {
            
            entity.ToTable("produse");
            entity.HasKey(e => e.IdProdus);

            entity.Property(e => e.IdProdus)
                .HasColumnName("id_produs")
                .HasColumnType("integer");

            entity.Property(e => e.CodProdus)
                .HasColumnName("cod_produs")
                .HasColumnType("varchar")
                .HasMaxLength(40)
                .IsRequired();

            entity.HasIndex(e => e.CodProdus)
                .IsUnique()
                .HasDatabaseName("Index_CodProdus");
            
            entity.Property(e => e.Descriere)
                .HasColumnType("varchar")
                .HasColumnName("descriere")
                .HasMaxLength(150);
            
            entity.Property(e => e.NumeProdus)
                .HasColumnType("varchar")
                .HasColumnName("nume_produs")
                .HasMaxLength(50);
            
            entity.Property(e => e.Compozitie)
                .HasColumnType("varchar")
                .HasColumnName("compozitie")
                .HasMaxLength(50);

            entity.Property(e => e.Tva)
                .HasColumnType("tinyint unsigned")
                .HasColumnName("TVA")
                .IsRequired();
            
            entity.Property(e => e.Ingrijire)
                .HasColumnType("varchar")
                .HasColumnName("ingrijire")
                .HasMaxLength(150);
            
            entity.Property(e => e.Greutate)
                .HasColumnType("decimal(4,2)")
                .HasColumnName("greutate")
                .HasPrecision(4,2);

            entity.Property(e => e.FataReversibila)
                .HasColumnType("tinyint")
                .HasColumnName("fata_reversbila");

            entity.Property(e => e.Stoc)
                .HasColumnType("smallint unsigned")
                .HasColumnName("stoc");

            entity.Property(e => e.TipulProdusului)
                .HasColumnType("varchar")
                .HasColumnName("tip_produs")
                .HasMaxLength(20)
                .IsRequired();
            
            entity.Property(e => e.IsDeleted)
                .HasColumnType("tinyint")
                .HasColumnName("isDeleted")
                .HasDefaultValue(false)
                .IsRequired();
            
            
            entity.Property(e => e.ActivInMagazin)
                .HasColumnType("tinyint")
                .HasColumnName("activ_in_magazin")
                .HasDefaultValue(true)
                .IsRequired();

            entity.Property(e => e.IdProducator)
                .HasColumnType("integer")
                .HasColumnName("id_producator");
            
            // One-To-Many mappings 


            entity.HasOne(p => p.Producator)
                .WithMany(pp => pp.ProducatoriProduse)
                .HasForeignKey(p => p.IdProducator)
                .HasConstraintName("FK_Producatori")
                .OnDelete(DeleteBehavior.SetNull);

        });
        
        modelBuilder.Entity<Seturi>(entity =>
        {
            entity.ToTable("seturi");
            entity.HasKey(e => e.IdSet);

            entity.Property(e => e.IdSet)
                .HasColumnType("integer")
                .HasColumnName("id_set")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            entity.Property(e => e.NumeSet)
                .HasColumnType("varchar")
                .HasColumnName("nume_set")
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.DescriereSet)
                .HasColumnType("varchar")
                .HasColumnName("descriere_set")
                .HasMaxLength(50)
                .IsRequired();
            
            
            
        });

        modelBuilder.Entity<Imagini>(entity =>
        {
            entity.ToTable("imagini");
            entity.HasKey(e => e.IdImagine);

            entity.Property(e => e.IdImagine)
                .HasColumnType("integer")
                .HasColumnName("id_imagine")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            entity.Property(e => e.IdProdusCuCuloare)
                .HasColumnName("id_produs_culoare")
                .HasColumnType("integer")
                .IsRequired();

            entity.Property(e => e.CaleImagine)
                .HasColumnType("varchar")
                .HasColumnName("cale_imagine")
                .HasMaxLength(100);

            entity.Property(e => e.FisierInBucket)
                .HasColumnType("varchar")
                .HasColumnName("bucket_directory")
                .HasMaxLength(70);


        });
        

        modelBuilder.Entity<Manopere>(entity =>
        {
            entity.ToTable("manopere");
            entity.HasKey(e => e.IdManopera);

            entity.Property(e => e.IdManopera)
                .HasColumnType("integer")
                .HasColumnName("id_manopera")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            entity.Property(e => e.IdInelPrindere)
                .HasColumnType("integer")
                .HasColumnName("id_inel_prindere");
            
            entity.Property(e => e.IdTipGalerie)
                .HasColumnType("integer")
                .HasColumnName("id_tip_galerie")
                .IsRequired();

            entity.Property(e => e.IdMaterial)
                .HasColumnType("integer")
                .HasColumnName("id_material")
                .IsRequired();
            
            entity.Property(e => e.IdTipLinie)
                .HasColumnType("integer")
                .HasColumnName("id_tip_linie")
                .IsRequired();

        });

        modelBuilder.Entity<TipuriProduse>(entity =>
        {
            entity.ToTable("tipuri_produse");
            entity.HasKey(e => e.IdTipProdus);

            entity.Property(e => e.IdTipProdus)
                .HasColumnType("integer")
                .HasColumnName("id_tip_pe_produs")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            entity.Property(e => e.TipProdus)
                .HasColumnType("varchar")
                .HasColumnName("tip_produs")
                .HasMaxLength(20)
                .IsRequired();
            
            entity.Property(e => e.Categorie)
                .HasColumnType("varchar")
                .HasColumnName("categorie")
                .HasMaxLength(40)
                .IsRequired();

            
        });

        modelBuilder.Entity<TipuriPeProduse>(entity =>
        {
            entity.ToTable("tipuri_pe_produse");
            entity.HasKey(e => e.IdTipPeProdus);

            entity.Property(e => e.IdTipPeProdus)
                .HasColumnType("integer")
                .HasColumnName("id_tip_pe_produs")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            entity.Property(e => e.IdProdus)
                .HasColumnName("id_produs")
                .HasColumnType("integer")
                .IsRequired();
            
            entity.Property(e => e.IdTipProdus)
                .HasColumnType("integer")
                .HasColumnName("id_tip_produs")
                .IsRequired();

            entity.HasOne(tpp => tpp.TppProdus)
                .WithMany(p => p.PTipuriPeProduse)
                .HasForeignKey(tpp => tpp.IdProdus)
                .HasConstraintName("FK_Produse_TPP")
                .IsRequired();
            
            entity.HasOne(tpp => tpp.TppTipProdus)
                .WithMany(p => p.TpTipuriPeProduse)
                .HasForeignKey(tpp => tpp.IdTipProdus)
                .HasConstraintName("FK_TipProduse_TPP")
                .IsRequired();
        });

        modelBuilder.Entity<AsociereSeturi>(entity =>
        {
            entity.ToTable("asociere_seturi");
            entity.HasKey(e => e.IdAsociereSet);

            entity.Property(e => e.IdAsociereSet)
                .HasColumnType("integer")
                .HasColumnName("id_asociere_set")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);
            
            entity.Property(e => e.IdProdus)
                .HasColumnName("id_produs")
                .HasColumnType("integer")
                .IsRequired();
            
            entity.Property(e => e.IdSet)
                .HasColumnType("integer")
                .HasColumnName("id_set")
                .IsRequired();

            entity.HasOne(asoc => asoc.Produs)
                .WithMany(prod => prod.PAsociereSeturi)
                .HasForeignKey(asoc => asoc.IdProdus)
                .HasConstraintName("FK_Produse_AS")
                .IsRequired();

            entity.HasOne(asoc => asoc.Set)
                .WithMany(set => set.SAsociereSeturi)
                .HasForeignKey(asoc => asoc.IdSet)
                .HasConstraintName("FK_Seturi_AS")
                .IsRequired();
        });
        
        modelBuilder.Entity<Vouchere>(entity =>
        {
            entity.ToTable("vouchere");
            entity.HasKey(e => e.IdVoucher);

            entity.Property(e => e.IdVoucher)
                .HasColumnType("integer")
                .HasColumnName("id_voucher")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);
            
            entity.Property(e => e.Reducere)
                .HasColumnType("decimal(2,2)")
                .HasColumnName("reducere")
                .HasPrecision(2,2)
                .IsRequired();
            
            entity.Property(e => e.CodVoucher)
                .HasColumnType("varchar")
                .HasColumnName("cod_voucher")
                .HasMaxLength(10)
                .IsRequired();
            
                
            entity.Property(e => e.DataExpirare)
                .HasColumnType("date")
                .HasColumnName("data_expirare")
                .IsRequired();
            
            
        });
        
        modelBuilder.Entity<ProduseCuVouchere>(entity =>
        {
            entity.ToTable("produse_cu_vouchere");
            entity.HasKey(e => e.IdProdusCuVoucher);

            entity.Property(e => e.IdProdusCuVoucher)
                .HasColumnType("integer")
                .HasColumnName("id_produs_cu_voucher")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);
            
            entity.Property(e => e.IdProdus)
                .HasColumnName("id_produs")
                .HasColumnType("integer")
                .IsRequired();
            
            entity.Property(e => e.IdVoucher)
                .HasColumnType("integer")
                .HasColumnName("id_voucher");

            entity.HasOne(pv => pv.PvProdus)
                .WithMany(p => p.PvProduse)
                .HasForeignKey(pv => pv.IdProdus)
                .HasConstraintName("FK_Produse_PV")
                .IsRequired();

            entity.HasOne(pv => pv.PvVoucher)
                .WithMany(p => p.VProduse)
                .HasForeignKey(pv => pv.IdVoucher)
                .HasConstraintName("FK_Vouchere_PV")
                .IsRequired();


        });


        modelBuilder.Entity<ProduseCuDimensiuni>(entity =>
        {
            entity.ToTable("dimensiuni_produse");
            entity.HasKey(e => e.IdProdusCuDimensiune);
            
            entity.Property(e => e.IdProdusCuDimensiune)
                .HasColumnType("integer")
                .HasColumnName("id_produs_cu_dimensiune")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);
            
            entity.Property(e => e.IdProdus)
                .HasColumnName("id_produs")
                .HasColumnType("integer")
                .IsRequired();
            
            entity.Property(e => e.Pret)
                .HasColumnType("decimal(6,2)")
                .HasColumnName("pret")
                .HasPrecision(6, 2);
            
            entity.Property(e => e.PretRedus)
                .HasColumnType("decimal")
                .HasColumnName("pret_redus")
                .HasPrecision(6,2)
                .HasDefaultValue(0m);
            entity.Property(e => e.IdDimensiune)
                .HasColumnName("id_dimensiune")
                .HasColumnType("integer");


            entity.HasOne(pd => pd.PdDimensiune)
                .WithMany(d => d.DProduseCuDimensiuni)
                .HasForeignKey(pd => pd.IdDimensiune)
                .HasConstraintName("FK_Dimensiuni_PD")
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(pd => pd.PdProduse)
                .WithMany(p=> p.PProduseCuDimensiuni)
                .HasForeignKey(pd => pd.IdProdus)
                .HasConstraintName("FK_Produse_PD")
                .IsRequired();
        });

        modelBuilder.Entity<InelePrindere>(entity =>
        {
            entity.ToTable("inele_prindere");
            entity.HasKey(e => e.IdInel);
            
            entity.Property(e => e.IdInel)
                .HasColumnType("integer")
                .HasColumnName("id_inel_prindere")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);
            
            entity.Property(e => e.CuloareInel)
                .HasColumnType("varchar")
                .HasColumnName("culoare_inel")
                .HasMaxLength(20)
                .IsRequired();
            
            entity.Property(e => e.PretPerMetruInele)
                .HasColumnType("decimal")
                .HasColumnName("pret_metru_inele")
                .HasPrecision(5,2)
                .IsRequired();

            entity.HasMany(ip => ip.InelPeManopere)
                .WithOne(man => man.InelPrindereLaManopera)
                .HasForeignKey(man => man.IdInelPrindere)
                .HasConstraintName("FK_Inele_Prindere")
                ;
            
        });

        
        modelBuilder.Entity<TipuriGalerie>(entity =>
        {
            entity.ToTable("tipuri_galerie");
            entity.HasKey(e => e.IdTipGalerie);
            
            entity.Property(e => e.IdTipGalerie)
                .HasColumnType("integer")
                .HasColumnName("id_tip_galerie")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);
            
            entity.Property(e => e.NumeTipGalerie)
                .HasColumnType("varchar")
                .HasColumnName("nume_galerie")
                .HasMaxLength(30)
                .IsRequired();
            
            entity.Property(e => e.PretTipGalerie)
                .HasColumnType("decimal")
                .HasColumnName("pret_metru_galerie")
                .HasPrecision(5,2)
                .IsRequired();

            entity.HasMany(tg => tg.TipGalerieManopere)
                .WithOne(man => man.TipGalerieLaManopera)
                .HasForeignKey(man => man.IdTipGalerie)
                .HasConstraintName("FK_Tip_Galerie")
                .IsRequired();
            
        });

        modelBuilder.Entity<TipuriLinie>(entity =>
        {
            entity.ToTable("tipuri_linie");
            entity.HasKey(e => e.IdTipLinie);
            
            entity.Property(e => e.IdTipLinie)
                .HasColumnType("integer")
                .HasColumnName("id_tip_linie")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);
            
            entity.Property(e => e.NumeTipLinie)
                .HasColumnType("varchar")
                .HasColumnName("nume_tip_linie")
                .HasMaxLength(30)
                .IsRequired();
            
            entity.Property(e => e.PretPeTipLinie)
                .HasColumnType("decimal")
                .HasColumnName("pret_metru_linie")
                .HasPrecision(5,2)
                .IsRequired();
            
            entity.HasMany(tl => tl.TipLiniePeManopere)
                .WithOne(man => man.TipLinieLaManopera)
                .HasForeignKey(man => man.IdTipLinie)
                .HasConstraintName("FK_Tip_Linie")
                .IsRequired();
            
        });
        
        modelBuilder.Entity<Materiale>(entity =>
        {
            entity.ToTable("materiale");
            entity.HasKey(e => e.IdMaterial);
            
            entity.Property(e => e.IdMaterial)
                .HasColumnType("integer")
                .HasColumnName("id_material")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);
            
            entity.Property(e => e.NumeMaterial)
                .HasColumnType("varchar")
                .HasColumnName("nume_material")
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.PretMaterial)
                .HasColumnType("decimal")
                .HasColumnName("pret_metru_material")
                .HasPrecision(5,2)
                .IsRequired();
            
            entity.Property(e => e.PretMaterialRedus)
                .HasColumnType("decimal")
                .HasColumnName("pret_metru_material_redus")
                .HasPrecision(5,2)
                .HasDefaultValue(0m)
                .IsRequired();
            
            entity.HasMany(mat => mat.MaterialPeManopere)
                .WithOne(man => man.MaterialLaManopere)
                .HasForeignKey(man => man.IdMaterial)
                .HasConstraintName("FK_Materiale")
                .IsRequired();
            
        });
        
        // de rulat migrarea , de modificat procesarea documentului excel.
    }
}