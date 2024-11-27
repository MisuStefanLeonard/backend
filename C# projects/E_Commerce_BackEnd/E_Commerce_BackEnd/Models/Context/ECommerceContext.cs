using E_Commerce_BackEnd.Models.Enums;
using E_Commerce_BackEnd.Models.OrderRelatedModels;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Models.ProductVouchersModels;
using E_Commerce_BackEnd.Models.UserRelatedModels;
using Microsoft.EntityFrameworkCore;
namespace E_Commerce_BackEnd.Models.Context;

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
   

    #endregion
   
    
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Locatii>(entity =>
        {
            entity.ToTable("locatii");
            entity.HasKey(e => e.IdLocatie);

            entity.Property(e => e.IdLocatie)
                .HasColumnType("int(1)")
                .HasColumnName("id_locatie");

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
                .HasColumnType("int(1)");
                

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

            entity.HasIndex(e => e.Username)
                .HasDatabaseName("INDEX_USERNAME")
                .IsUnique();
            
            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasColumnType("varchar")
                .HasMaxLength(50)
                .IsRequired();
            
            entity.HasIndex(e => e.Email)
                .HasDatabaseName("INDEX_EMAIL")
                .IsUnique();
            
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
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RememberUser>(entity =>
        {
            entity.ToTable("sesiuni");
            entity.HasKey(e => e.IdSesiune);

            entity.Property(e => e.IdSesiune)
                .HasColumnName("id_sesiune")
                .HasColumnType("int(1)");
                

            entity.Property(e => e.IdCont)
                .HasColumnName("id_cont")
                .HasColumnType("integer")
                .IsRequired();

            entity.Property(e => e.SessionToken)
                .HasColumnType("varchar")
                .HasColumnName("sesiune_stocata")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.IssuedAt)
                .HasColumnType("datetime")
                .HasColumnName("issued_at")
                .HasDefaultValueSql("NOW()")
                .IsRequired();
            
            entity.Property(e => e.ExpiresAt)
                .HasColumnType("datetime")
                .HasColumnName("expires_at")
                .HasDefaultValueSql("(NOW() + INTERVAL 30 DAY)")
                .IsRequired();

            entity.HasOne(e => e.CurrentUserSession)
                .WithOne(c => c.RememberUserSession)
                .HasForeignKey<RememberUser>(e => e.IdCont)
                .IsRequired();

        });
        
        modelBuilder.Entity<Adrese>(entity =>
        {
            entity.ToTable("adrese");
            entity.HasKey(e => e.IdAdresa);

            entity.Property(e => e.IdAdresa)
                .HasColumnName("id_adresa")
                .HasColumnType("int(1)");
                

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
                .HasColumnType("integer")
                .IsRequired();
            
            entity.Property(e => e.IdLocatie)
                .HasColumnName("id_locatie")
                .HasColumnType("integer")
                .IsRequired();

            entity.Property(e => e.IsDeleted)
                .HasColumnName("isDeleted")
                .HasColumnType("tinyint")
                .HasDefaultValue(false)
                .IsRequired();
            
            entity.Property(e => e.IdDetaliuFactura)
                .HasColumnName("id_detaliu_factura")
                .HasColumnType("integer");
            
            entity.HasMany(a => a.AdreseLivrarePeComanda)
                .WithOne(c => c.CAdresaLivrare)
                .HasForeignKey(c => c.IdAdresaLivrare)
                .HasConstraintName("FK_Adresa_Livrare")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            
            entity.HasMany(a => a.AdreseFacturarePeComanda)
                .WithOne(c => c.CAdresaFacturare)
                .HasForeignKey(c => c.IdAdresaFacturare)
                .HasConstraintName("FK_Adresa_Facturare")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

        });
        
        modelBuilder.Entity<DetaliiFactura>(entity =>
        {
            entity.ToTable("detalii_factura");
            entity.HasKey(e => e.IdDetaliu);

            entity.Property(e => e.IdDetaliu)
                .HasColumnName("id_detaliu")
                .HasColumnType("int(1)");
                

            entity.Property(e => e.Cif)
                .HasColumnType("varchar")
                .HasColumnName("CIF")
                .HasMaxLength(12);

            entity.Property(e => e.NumeFirma)
                .HasColumnType("varchar")
                .HasColumnName("nume_firma")
                .HasMaxLength(50);
            
            entity.HasMany(df => df.DfAdrese)
                .WithOne(a => a.DetaliuFactura)
                .HasForeignKey(a => a.IdDetaliuFactura)
                .HasConstraintName("FK_DetaliiFactura_Adresa")
                .OnDelete(DeleteBehavior.Cascade);

        });

        
        modelBuilder.Entity<Comenzi>(entity =>
        {
            entity.ToTable("comenzi");
            entity.HasKey(e => e.IdComanda);

            entity.Property(e => e.IdComanda)
                .HasColumnType("int(1)")
                .HasColumnName("id_comanda");

         
            entity.Property(e => e.IdAdresaFacturare)
                .HasColumnName("id_adresa_facturare")
                .HasColumnType("integer")
                .IsRequired();
            
            entity.Property(e => e.IdAdresaLivrare)
                .HasColumnName("id_adresa_livrare")
                .HasColumnType("integer")
                .IsRequired();

            entity.Property(e => e.IdVoucher)
                .HasColumnName("id_voucher")
                .HasColumnType("integer");
           
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
            
            entity.Property(e => e.AwbComanda)
                .HasColumnName("awb_fan_courier")
                .HasColumnType("varchar")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.IsCancelable)
                .HasColumnType("is_cancelable")
                .HasColumnType("tinyint")
                .HasDefaultValue(true)
                .IsRequired();
                

            entity.HasOne(e => e.VoucherPeComanda)
                .WithMany(v => v.VVoucherePeComenzi)
                .HasForeignKey(e => e.IdVoucher)
                .HasConstraintName("FK_Voucher_Comanda");


        });

        modelBuilder.Entity<ProduseCuComenzi>(entity =>
        {
            entity.ToTable("produse_cu_comenzi");
            entity.HasKey(e => e.IdProduseCuComenzi);
            
            entity.Property(e => e.IdProduseCuComenzi)
                .HasColumnName("id_produse_cu_comenzi")
                .HasColumnType("int(1)");
                

            entity.Property(e => e.IdComanda)
                .HasColumnName("id_comanda")
                .HasColumnType("integer")
                .IsRequired();

            entity.Property(e => e.NrBucati)
                .HasColumnName("nr_buc")
                .HasColumnType("integer")
                .IsRequired();
            
            entity.Property(e => e.PretCumparat)
                .HasColumnType("decimal")
                .HasColumnName("pret_baza")
                .HasPrecision(6, 2)
                .IsRequired();
            
            entity.Property(e => e.IdCuloare)
                .HasColumnName("id_culoare")
                .HasColumnType("integer")
                .IsRequired();

            entity.Property(e => e.IdDimensiune)
                .HasColumnName("id_dimensiune")
                .HasColumnType("integer");
            
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
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.Property(e => e.IdSet)
                .HasColumnName("id_set")
                .HasColumnType("integer");
            
            entity.HasOne(pc => pc.Set)
                .WithMany(s => s.CombinatieSetPeComanda)
                .HasForeignKey(pc => pc.IdSet)
                .HasConstraintName("FK_Produse_Set")
                .OnDelete(DeleteBehavior.Cascade);
            
               // de rulat migrarea si de verificat dupa 
            entity.HasOne(pc => pc.Comanda)
                .WithMany(c => c.PcComenzi)
                .HasForeignKey(pc => pc.IdComanda)
                .HasConstraintName("FK_Comenzi_PC")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });
        
        modelBuilder.Entity<Dimensiuni>(entity =>
        {
         
            entity.ToTable("dimensiuni");
            entity.HasKey(e => e.IdDimensiune);

            entity.Property(e => e.IdDimensiune)
                .HasColumnName("id_dimensiune")
                .HasColumnType("int(1)");
            

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
            
           

            entity.HasMany(e => e.DAsociereSeturi)
                .WithOne(asoc => asoc.AsDimensiune)
                .HasForeignKey(asoc => asoc.IdDimensiune)
                .HasConstraintName("FK_Dimensiune_AsociereSeturi");

            entity.HasMany(e => e.DProduseCuComenzi)
                .WithOne(pc => pc.PcDimensiune)
                .HasForeignKey(pc => pc.IdDimensiune)
                .HasConstraintName("FK_Dimensiune_ProduseComenzi");


        });
        
        modelBuilder.Entity<Producatori>(entity =>
        {
            entity.ToTable("producatori");
            entity.HasKey(e => e.IdProducator);

            entity.Property(e => e.IdProducator)
                .HasColumnName("id_producator")
                .HasColumnType("int(1)");

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
                .HasColumnType("int(1)")
                .HasColumnName("id_cod_culoare");

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
                .HasColumnName("id_culoare")
                .HasColumnType("int(1)");

            entity.Property(e => e.NumeCuloare)
                .HasColumnName("nume_culoare")
                .HasColumnType("varchar")
                .HasMaxLength(20)
                .IsRequired();
            
            entity.Property(e => e.IdCodCuloare)
                .HasColumnName("id_cod_culoare")
                .HasColumnType("integer")
                .IsRequired();

            entity.HasMany(e => e.CAsociereSeturi)
                .WithOne(asoc => asoc.AsCuloare)
                .HasForeignKey(asoc => asoc.IdCuloare)
                .HasConstraintName("FK_Culoare_AsociereSeturi");
            
            entity.HasMany(e => e.CProduseCuComenzi)
                .WithOne(pc => pc.PcCuloare)
                .HasForeignKey(pc => pc.IdCuloare)
                .HasConstraintName("FK_Culoare_ProduseComenzoi")
                .IsRequired();


        });

        modelBuilder.Entity<ProduseCuCulori>(entity =>
        {
            entity.ToTable("culori_produse");
            entity.HasKey(e => e.IdProdusCuCuloare);
            
            
            entity.Property(e => e.IdProdusCuCuloare)
                .HasColumnName("id_produs_culoare")
                .HasColumnType("int(1)");

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
                .HasColumnType("int(1)");

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
            
            entity.Property(e => e.PretDeBaza)
                .HasColumnType("decimal")
                .HasColumnName("pret_baza")
                .HasPrecision(6, 2);
            
            entity.Property(e => e.PretDeBazaRedus)
                .HasColumnType("decimal")
                .HasColumnName("pret_baza_redus")
                .HasPrecision(6, 2);
            
            
            entity.Property(e => e.ActivInMagazin)
                .HasColumnType("tinyint")
                .HasColumnName("activ_in_magazin")
                .HasDefaultValue(true)
                .IsRequired();
            

            entity.Property(e => e.IdProducator)
                .HasColumnType("integer")
                .HasColumnName("id_producator");
            
            entity.Property(e => e.AfiseazaInNoutati)
                .HasColumnType("tinyint")
                .HasColumnName("afiseaza_in_noutati")
                .HasDefaultValue(false)
                .IsRequired();
            
            entity.Property(e => e.ProdusLimitat)
                .HasColumnType("tinyint")
                .HasColumnName("produs_limitat")
                .HasDefaultValue(false)
                .IsRequired();
            
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
                .HasColumnName("id_set")
                .HasColumnType("int(1)");

            entity.Property(e => e.NumeSet)
                .HasColumnType("varchar")
                .HasColumnName("nume_set")
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.DescriereSet)
                .HasColumnType("varchar")
                .HasColumnName("descriere_set")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(e => e.PretRedusSet)
                .HasColumnType("decimal")
                .HasColumnName("pret_set_redus")
                .HasPrecision(6, 2);
            
            entity.Property(e => e.PretSet)
                .HasColumnType("decimal")
                .HasColumnName("pret_set")
                .HasPrecision(6, 2);

            entity.Property(e => e.SetActivInMagazin)
                .HasColumnType("tinyint")
                .HasColumnName("set_activ")
                .IsRequired();

            entity.Property(e => e.IsDeleted)
                .HasColumnName("isDeleted")
                .HasColumnType("tinyint")
                .HasDefaultValue(false)
                .IsRequired();

        });

        modelBuilder.Entity<Imagini>(entity =>
        {
            entity.ToTable("imagini");
            entity.HasKey(e => e.IdImagine);

            entity.Property(e => e.IdImagine)
                .HasColumnName("id_imagine")
                .HasColumnType("int(1)");

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
                .HasColumnName("id_manopera")
                .HasColumnType("int(1)");

            entity.Property(e => e.NumeManopera)
                .HasColumnType("varchar(70)")
                .HasColumnName("nume_manopera")
                .HasMaxLength(70);

            entity.Property(e => e.IdInelPrindere)
                .HasColumnType("integer")
                .HasColumnName("id_inel_prindere");
            
            entity.Property(e => e.IdTipGalerie)
                .HasColumnType("integer")
                .HasColumnName("id_tip_galerie")
                .IsRequired();
            
            entity.Property(e => e.IdTipLinie)
                .HasColumnType("integer")
                .HasColumnName("id_tip_linie")
                .IsRequired();

            entity.Property(e => e.PretCurentTipGalerie)
                .HasColumnType("decimal")
                .HasPrecision(6, 2)
                .HasColumnName("pret_curent_rejansa")
                .IsRequired();
            
            entity.Property(e => e.PretCurentTipLinie)
                .HasColumnType("decimal")
                .HasPrecision(6, 2)
                .HasColumnName("pret_curent_tip_linie")
                .IsRequired();
            
            entity.Property(e => e.MaterialFolosit)
                .HasColumnType("decimal")
                .HasPrecision(6, 2)
                .HasColumnName("material_folosit")
                .IsRequired();
            
            
            entity.Property(e => e.TipManopera)
                .HasColumnName("tip_manopera")
                .HasConversion<string>()
                .HasDefaultValue(TipManopere.Aleasa)
                .HasMaxLength(10)
                .IsRequired();

            entity.HasMany(e => e.ManopereCuComenzi)
                .WithOne(pc => pc.PcManopera)
                .HasForeignKey(pc => pc.IdManopera)
                .HasConstraintName("FK_Manopera_ProduseComenzi");
            
            entity.HasMany(e => e.ManopereStandardPeSet)
                .WithOne(asoc => asoc.Manopera)
                .HasForeignKey(asoc => asoc.IdManopera)
                .HasConstraintName("FK_Manopera_AsociereSeturi");
            

        });

        modelBuilder.Entity<TipuriProduse>(entity =>
        {
            entity.ToTable("tipuri_produse");
            entity.HasKey(e => e.IdTipProdus);

            entity.Property(e => e.IdTipProdus)
                .HasColumnName("id_tip_pe_produs")
                .HasColumnType("int(1)");
            
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
               .HasColumnName("id_tip_pe_produs")
                .HasColumnType("int(1)");

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
                .HasColumnName("id_asociere_set")
                .HasColumnType("int(1)");
            
            entity.Property(e => e.IdProdus)
                .HasColumnName("id_produs")
                .HasColumnType("integer")
                .IsRequired();
            
            entity.Property(e => e.IdSet)
                .HasColumnType("integer")
                .HasColumnName("id_set")
                .IsRequired();

            entity.Property(e => e.IdDimensiune)
                .HasColumnType("integer")
                .HasColumnName("id_dimensiune");

            entity.Property(e => e.IdCuloare)
                .HasColumnType("integer")
                .HasColumnName("id_culoare");
            
            entity.Property(e => e.IdManopera)
                .HasColumnType("integer")
                .HasColumnName("id_manopera");

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
                .HasColumnName("id_voucher")
                .HasColumnType("int(1)");
            
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
            
            entity.Property(e => e.IsDeleted)
                .HasColumnName("isDeleted")
                .HasColumnType("tinyint")
                .HasDefaultValue(false)
                .IsRequired();
            
            
        });
        
       
        modelBuilder.Entity<ProduseCuDimensiuni>(entity =>
        {
            entity.ToTable("dimensiuni_produse");
            entity.HasKey(e => e.IdProdusCuDimensiune);
            
            entity.Property(e => e.IdProdusCuDimensiune)
                .HasColumnName("id_produs_cu_dimensiune")
                .HasColumnType("int(1)");
            
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
                .HasColumnName("id_inel_prindere")
                .HasColumnType("int(1)");
            
            entity.Property(e => e.CuloareInel)
                .HasColumnType("varchar")
                .HasColumnName("culoare_inel")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.CaleRelativa)
                .HasColumnType("varchar")
                .HasColumnName("cale_relativa")
                .HasMaxLength(100);
            
            
            entity.Property(e => e.IsDeleted)
                .HasColumnName("isDeleted")
                .HasColumnType("tinyint")
                .HasDefaultValue(false)
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
                .HasColumnName("id_tip_galerie")
                .HasColumnType("int(1)");
            
            entity.Property(e => e.NumeTipGalerie)
                .HasColumnType("varchar")
                .HasColumnName("nume_galerie")
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(e => e.CaleRelativa)
                .HasColumnType("varchar")
                .HasColumnName("cale_relativa")
                .HasMaxLength(100);
            
            entity.Property(e => e.PretTipGalerie)
                .HasColumnType("decimal")
                .HasColumnName("pret_metru_galerie")
                .HasPrecision(5,2)
                .IsRequired();
            
            entity.Property(e => e.IncretireRejansa)
                .HasColumnType("decimal")
                .HasColumnName("incretire")
                .HasPrecision(5,2)
                .IsRequired();

            
            entity.Property(e => e.IsDeleted)
                .HasColumnName("isDeleted")
                .HasColumnType("tinyint")
                .HasDefaultValue(false)
                .IsRequired();
            
            entity.Property(e => e.SePrindeCuInele)
                .HasColumnName("prindere_inele")
                .HasColumnType("tinyint")
                .HasDefaultValue(false)
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
                .HasColumnName("id_tip_linie")
                .HasColumnType("int(1)");
            
            entity.Property(e => e.NumeTipLinie)
                .HasColumnType("varchar")
                .HasColumnName("nume_tip_linie")
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(e => e.CaleRelativa)
                .HasColumnType("varchar")
                .HasColumnName("cale_relativa")
                .HasMaxLength(100);
            
            entity.Property(e => e.PretPeTipLinie)
                .HasColumnType("decimal")
                .HasColumnName("pret_metru_linie")
                .HasPrecision(5,2)
                .IsRequired();
            
            entity.Property(e => e.IsDeleted)
                .HasColumnName("isDeleted")
                .HasColumnType("tinyint")
                .HasDefaultValue(false)
                .IsRequired();
            
            entity.HasMany(tl => tl.TipLiniePeManopere)
                .WithOne(man => man.TipLinieLaManopera)
                .HasForeignKey(man => man.IdTipLinie)
                .HasConstraintName("FK_Tip_Linie")
                .IsRequired();
            
        });
        
        modelBuilder.Entity<CosCumparaturi>(entity =>
        {
            entity.ToTable("cos_cumparaturi");
            entity.HasKey(e => e.IdProdusInCos);
            
            entity.Property(e => e.IdProdusInCos)
                .HasColumnName("id_produs_in_cos")
                .HasColumnType("int(1)");
            
            entity.Property(e => e.CantitateProdus)
                .HasColumnType("int")
                .HasColumnName("cantitate_produs")
                .IsRequired();
            
            entity.Property(e => e.PretProdus)
                .HasColumnType("decimal")
                .HasColumnName("pret_produs")
                .HasPrecision(6,2)
                .IsRequired();
            
            entity.Property(e => e.IdCont)
                .HasColumnType("int")
                .HasColumnName("id_cont")
                .IsRequired();
        
            entity.Property(e => e.IdDimensiune)
                .HasColumnType("int")
                .HasColumnName("id_dimensiune")
                .IsRequired();
        
            entity.Property(e => e.IdCuloare)
                .HasColumnType("int")
                .HasColumnName("id_culoare")
                .IsRequired();

            entity.Property(e => e.IdManopera)
                .HasColumnType("int")
                .HasColumnName("id_manopera");
            
            entity.Property(e => e.IdSet)
                .HasColumnType("int")
                .HasColumnName("id_set");
            
             entity.Property(e => e.IdProdus)
                .HasColumnType("int")
                .HasColumnName("id_produs")
                .IsRequired();

             entity.HasOne(cont => cont.Cont)
                 .WithMany(cont => cont.ProduseInCosPeCont)
                 .HasForeignKey(cosCump => cosCump.IdCont)
                 .IsRequired();
             
             entity.HasOne(cosCump => cosCump.Produs)
                 .WithMany(produs => produs.ProduseInCos)
                 .HasForeignKey(cosCump => cosCump.IdProdus)
                 .IsRequired();
             
             entity.HasOne(cosCump => cosCump.Culoare)
                 .WithMany(culoare => culoare.CuloriPeCosCumparaturi)
                 .HasForeignKey(cosCump => cosCump.IdCuloare)
                 .IsRequired();
            
             entity.HasOne(cosCump => cosCump.Dimensiune)
                 .WithMany(dimensiune => dimensiune.DPeCosCumparaturi)
                 .HasForeignKey(cosCump => cosCump.IdDimensiune)
                 .IsRequired();

             entity.HasOne(cosCump => cosCump.Set)
                 .WithMany(set => set.SeturiPeCos)
                 .HasForeignKey(cosCump => cosCump.IdSet);
             
             entity.HasOne(cosCump => cosCump.Manopera)
                 .WithMany(manopera => manopera.ManoperePeCos)
                 .HasForeignKey(cosCump => cosCump.IdManopera);
        });

        modelBuilder.Entity<Reviews>(entity =>
        {
            entity.ToTable("reviews");
            entity.HasKey(e => e.IdRecenzie);

            entity.Property(e => e.IdRecenzie)
                .HasColumnName("id_review")
                .HasColumnType("int(1)");

            entity.Property(e => e.IdCont)
                .HasColumnName("id_cont")
                .HasColumnType("integer")
                .IsRequired();

            entity.Property(e => e.IdProdus)
                .HasColumnName("id_produs")
                .HasColumnType("integer");


            entity.Property(e => e.IdSet)
                .HasColumnName("id_set")
                .HasColumnType("integer");

            entity.Property(e => e.NumarStele)
                .HasColumnName("numar_stele")
                .HasColumnType("integer")
                .IsRequired();
            
            entity.Property(e => e.TextRecenzie)
                .HasColumnName("text_recenzie")
                .HasColumnType("varchar")
                .HasMaxLength(150)
                .IsRequired();


            entity.HasOne(r => r.Cont)
                .WithMany(acc => acc.ReviewsProduse)
                .HasForeignKey(r => r.IdCont)
                .IsRequired();

            entity.HasOne(r => r.Produs)
                .WithMany(acc => acc.ProductReviews)
                .HasForeignKey(r => r.IdProdus);
            
            entity.HasOne(r => r.Set)
                .WithMany(s => s.ReviewPeSet)
                .HasForeignKey(r => r.IdSet);
            
        });

    }
}