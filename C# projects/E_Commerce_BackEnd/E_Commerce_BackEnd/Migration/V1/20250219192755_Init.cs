using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_Commerce_BackEnd.Migration.V1
{
    /// <inheritdoc />
    public partial class Init : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "cod_culori",
                columns: table => new
                {
                    id_cod_culoare = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    cod_culoare = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cod_culori", x => x.id_cod_culoare);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "conturi",
                columns: table => new
                {
                    id_cont = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nume = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    prenume = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gen = table.Column<sbyte>(type: "tinyint", nullable: true),
                    nr_telefon = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    username = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    parola = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    data_creare = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "NOW()"),
                    cod_activare = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    verificat = table.Column<sbyte>(type: "tinyint", nullable: false),
                    guest = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    rol = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ora_link = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conturi", x => x.id_cont);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "detalii_factura",
                columns: table => new
                {
                    id_detaliu = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CIF = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nume_firma = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detalii_factura", x => x.id_detaliu);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "dimensiuni",
                columns: table => new
                {
                    id_dimensiune = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    lungime = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    latime = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pereche_perdea = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    recomandare_pat = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dimensiuni", x => x.id_dimensiune);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "global_config",
                columns: table => new
                {
                    id_config = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nume_atribut = table.Column<string>(type: "varchar(100)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    valoare_atribut = table.Column<string>(type: "varchar(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_global_config", x => x.id_config);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "inele_prindere",
                columns: table => new
                {
                    id_inel_prindere = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    culoare_inel = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cale_relativa = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    isDeleted = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    is_locked = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inele_prindere", x => x.id_inel_prindere);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "locatii",
                columns: table => new
                {
                    id_locatie = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    oras = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    judet = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cod_postal = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locatii", x => x.id_locatie);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "producatori",
                columns: table => new
                {
                    id_producator = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nume_producator = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_producatori", x => x.id_producator);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "seturi",
                columns: table => new
                {
                    id_set = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nume_set = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descriere_set = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pret_set = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    pret_set_redus = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    set_activ = table.Column<sbyte>(type: "tinyint", nullable: false),
                    is_locked = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    isDeleted = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seturi", x => x.id_set);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tipuri_galerie",
                columns: table => new
                {
                    id_tip_galerie = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nume_galerie = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    incretire = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    pret_metru_galerie = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    isDeleted = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    is_locked = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    prindere_inele = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    cale_relativa = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipuri_galerie", x => x.id_tip_galerie);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tipuri_linie",
                columns: table => new
                {
                    id_tip_linie = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nume_tip_linie = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pret_metru_linie = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    cale_relativa = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    isDeleted = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    is_locked = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipuri_linie", x => x.id_tip_linie);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tipuri_produse",
                columns: table => new
                {
                    id_tip_pe_produs = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    categorie = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipuri_produse", x => x.id_tip_pe_produs);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "vouchere",
                columns: table => new
                {
                    id_voucher = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    cod_voucher = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reducere = table.Column<decimal>(type: "decimal(2,2)", precision: 2, scale: 2, nullable: false),
                    data_expirare = table.Column<DateTime>(type: "date", nullable: false),
                    isDeleted = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vouchere", x => x.id_voucher);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "culori",
                columns: table => new
                {
                    id_culoare = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nume_culoare = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_cod_culoare = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_culori", x => x.id_culoare);
                    table.ForeignKey(
                        name: "FK_CodCulori",
                        column: x => x.id_cod_culoare,
                        principalTable: "cod_culori",
                        principalColumn: "id_cod_culoare",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sesiuni",
                columns: table => new
                {
                    id_sesiune = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_cont = table.Column<int>(type: "integer", nullable: false),
                    sesiune_stocata = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    issued_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "NOW()"),
                    expires_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(NOW() + INTERVAL 30 DAY)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sesiuni", x => x.id_sesiune);
                    table.ForeignKey(
                        name: "FK_sesiuni_conturi_id_cont",
                        column: x => x.id_cont,
                        principalTable: "conturi",
                        principalColumn: "id_cont",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "adrese",
                columns: table => new
                {
                    id_adresa = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    alias = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tip_adresa = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    bloc = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nr_bloc = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    strada = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nr_strada = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    isDeleted = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    id_locatie = table.Column<int>(type: "integer", nullable: false),
                    id_cont = table.Column<int>(type: "integer", nullable: false),
                    id_detaliu_factura = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adrese", x => x.id_adresa);
                    table.ForeignKey(
                        name: "FK_Conturi",
                        column: x => x.id_cont,
                        principalTable: "conturi",
                        principalColumn: "id_cont",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetaliiFactura_Adresa",
                        column: x => x.id_detaliu_factura,
                        principalTable: "detalii_factura",
                        principalColumn: "id_detaliu",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Locatii",
                        column: x => x.id_locatie,
                        principalTable: "locatii",
                        principalColumn: "id_locatie",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "produse",
                columns: table => new
                {
                    id_produs = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    cod_produs = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descriere = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nume_produs = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    compozitie = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TVA = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    ingrijire = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fata_reversbila = table.Column<sbyte>(type: "tinyint", nullable: true),
                    stoc = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    tip_produs = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    isDeleted = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    activ_in_magazin = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)1),
                    pret_baza = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    pret_baza_redus = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    is_locked = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    afiseaza_in_noutati = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    produs_limitat = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    id_producator = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_produse", x => x.id_produs);
                    table.ForeignKey(
                        name: "FK_Producatori",
                        column: x => x.id_producator,
                        principalTable: "producatori",
                        principalColumn: "id_producator",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "manopere",
                columns: table => new
                {
                    id_manopera = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nume_manopera = table.Column<string>(type: "varchar(70)", maxLength: 70, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_inel_prindere = table.Column<int>(type: "integer", nullable: true),
                    id_tip_linie = table.Column<int>(type: "integer", nullable: false),
                    id_tip_galerie = table.Column<int>(type: "integer", nullable: false),
                    pret_curent_tip_linie = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    pret_curent_rejansa = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    material_folosit = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    tip_manopera = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, defaultValue: "Aleasa")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_locked = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    inaltime_maxima = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manopere", x => x.id_manopera);
                    table.ForeignKey(
                        name: "FK_Inele_Prindere",
                        column: x => x.id_inel_prindere,
                        principalTable: "inele_prindere",
                        principalColumn: "id_inel_prindere");
                    table.ForeignKey(
                        name: "FK_Tip_Galerie",
                        column: x => x.id_tip_galerie,
                        principalTable: "tipuri_galerie",
                        principalColumn: "id_tip_galerie",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tip_Linie",
                        column: x => x.id_tip_linie,
                        principalTable: "tipuri_linie",
                        principalColumn: "id_tip_linie",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "comenzi",
                columns: table => new
                {
                    id_comanda = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    data_emitere_comanda = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "NOW()"),
                    status_comanda = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tip_plata = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    awb_fan_courier = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pret_transport = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    nume_pe_comanda = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    prenume_pe_comanda = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nr_telefon_pe_comanda = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email_pe_comanda = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    confirmation_token = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_confirmation_token_used = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    is_order_payed = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    IsCancelable = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)1),
                    id_adresa_livrare = table.Column<int>(type: "integer", nullable: false),
                    id_adresa_facturare = table.Column<int>(type: "integer", nullable: false),
                    id_voucher = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comenzi", x => x.id_comanda);
                    table.ForeignKey(
                        name: "FK_Adresa_Facturare",
                        column: x => x.id_adresa_facturare,
                        principalTable: "adrese",
                        principalColumn: "id_adresa",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Adresa_Livrare",
                        column: x => x.id_adresa_livrare,
                        principalTable: "adrese",
                        principalColumn: "id_adresa",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Voucher_Comanda",
                        column: x => x.id_voucher,
                        principalTable: "vouchere",
                        principalColumn: "id_voucher");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "culori_produse",
                columns: table => new
                {
                    id_produs_culoare = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_produs = table.Column<int>(type: "integer", nullable: false),
                    id_culoare = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_culori_produse", x => x.id_produs_culoare);
                    table.ForeignKey(
                        name: "FK_Culori_ProduseCuCulori",
                        column: x => x.id_culoare,
                        principalTable: "culori",
                        principalColumn: "id_culoare",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Produse_ProduseCuCulori",
                        column: x => x.id_produs,
                        principalTable: "produse",
                        principalColumn: "id_produs",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "dimensiuni_produse",
                columns: table => new
                {
                    id_produs_cu_dimensiune = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    pret = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    pret_redus = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false, defaultValue: 0m),
                    id_dimensiune = table.Column<int>(type: "integer", nullable: true),
                    id_produs = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dimensiuni_produse", x => x.id_produs_cu_dimensiune);
                    table.ForeignKey(
                        name: "FK_Dimensiuni_PD",
                        column: x => x.id_dimensiune,
                        principalTable: "dimensiuni",
                        principalColumn: "id_dimensiune",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Produse_PD",
                        column: x => x.id_produs,
                        principalTable: "produse",
                        principalColumn: "id_produs",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    id_review = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_produs = table.Column<int>(type: "integer", nullable: true),
                    id_cont = table.Column<int>(type: "integer", nullable: false),
                    id_set = table.Column<int>(type: "integer", nullable: true),
                    numar_stele = table.Column<int>(type: "integer", nullable: false),
                    text_recenzie = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reviews", x => x.id_review);
                    table.ForeignKey(
                        name: "FK_reviews_conturi_id_cont",
                        column: x => x.id_cont,
                        principalTable: "conturi",
                        principalColumn: "id_cont",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_reviews_produse_id_produs",
                        column: x => x.id_produs,
                        principalTable: "produse",
                        principalColumn: "id_produs");
                    table.ForeignKey(
                        name: "FK_reviews_seturi_id_set",
                        column: x => x.id_set,
                        principalTable: "seturi",
                        principalColumn: "id_set");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tipuri_pe_produse",
                columns: table => new
                {
                    id_tip_pe_produs = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_tip_produs = table.Column<int>(type: "integer", nullable: false),
                    id_produs = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipuri_pe_produse", x => x.id_tip_pe_produs);
                    table.ForeignKey(
                        name: "FK_Produse_TPP",
                        column: x => x.id_produs,
                        principalTable: "produse",
                        principalColumn: "id_produs",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TipProduse_TPP",
                        column: x => x.id_tip_produs,
                        principalTable: "tipuri_produse",
                        principalColumn: "id_tip_pe_produs",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "asociere_seturi",
                columns: table => new
                {
                    id_asociere_set = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_produs = table.Column<int>(type: "integer", nullable: false),
                    id_set = table.Column<int>(type: "integer", nullable: false),
                    id_culoare = table.Column<int>(type: "integer", nullable: true),
                    id_dimensiune = table.Column<int>(type: "integer", nullable: true),
                    id_manopera = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asociere_seturi", x => x.id_asociere_set);
                    table.ForeignKey(
                        name: "FK_Culoare_AsociereSeturi",
                        column: x => x.id_culoare,
                        principalTable: "culori",
                        principalColumn: "id_culoare");
                    table.ForeignKey(
                        name: "FK_Dimensiune_AsociereSeturi",
                        column: x => x.id_dimensiune,
                        principalTable: "dimensiuni",
                        principalColumn: "id_dimensiune");
                    table.ForeignKey(
                        name: "FK_Manopera_AsociereSeturi",
                        column: x => x.id_manopera,
                        principalTable: "manopere",
                        principalColumn: "id_manopera");
                    table.ForeignKey(
                        name: "FK_Produse_AS",
                        column: x => x.id_produs,
                        principalTable: "produse",
                        principalColumn: "id_produs",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Seturi_AS",
                        column: x => x.id_set,
                        principalTable: "seturi",
                        principalColumn: "id_set",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "cos_cumparaturi",
                columns: table => new
                {
                    id_produs_in_cos = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    cantitate_produs = table.Column<int>(type: "int", nullable: false),
                    pret_produs = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    inaltime_aleasa = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    identificator_set = table.Column<string>(type: "varchar(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    expires_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "NOW()"),
                    SessionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    id_cont = table.Column<int>(type: "int", nullable: true),
                    id_produs = table.Column<int>(type: "int", nullable: false),
                    id_culoare = table.Column<int>(type: "int", nullable: false),
                    id_dimensiune = table.Column<int>(type: "int", nullable: true),
                    id_set = table.Column<int>(type: "int", nullable: true),
                    id_manopera = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cos_cumparaturi", x => x.id_produs_in_cos);
                    table.ForeignKey(
                        name: "FK_cos_cumparaturi_conturi_id_cont",
                        column: x => x.id_cont,
                        principalTable: "conturi",
                        principalColumn: "id_cont");
                    table.ForeignKey(
                        name: "FK_cos_cumparaturi_culori_id_culoare",
                        column: x => x.id_culoare,
                        principalTable: "culori",
                        principalColumn: "id_culoare",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cos_cumparaturi_dimensiuni_id_dimensiune",
                        column: x => x.id_dimensiune,
                        principalTable: "dimensiuni",
                        principalColumn: "id_dimensiune");
                    table.ForeignKey(
                        name: "FK_cos_cumparaturi_manopere_id_manopera",
                        column: x => x.id_manopera,
                        principalTable: "manopere",
                        principalColumn: "id_manopera");
                    table.ForeignKey(
                        name: "FK_cos_cumparaturi_produse_id_produs",
                        column: x => x.id_produs,
                        principalTable: "produse",
                        principalColumn: "id_produs",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cos_cumparaturi_seturi_id_set",
                        column: x => x.id_set,
                        principalTable: "seturi",
                        principalColumn: "id_set");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "produse_cu_comenzi",
                columns: table => new
                {
                    id_produse_cu_comenzi = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nr_buc = table.Column<int>(type: "integer", nullable: false),
                    pret_baza = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    inaltime_set = table.Column<string>(type: "varchar(4)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    identificator_set = table.Column<string>(type: "varchar(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_set = table.Column<int>(type: "integer", nullable: true),
                    id_produs = table.Column<int>(type: "integer", nullable: false),
                    id_comanda = table.Column<int>(type: "integer", nullable: false),
                    id_culoare = table.Column<int>(type: "integer", nullable: false),
                    id_dimensiune = table.Column<int>(type: "integer", nullable: true),
                    id_manopera = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_produse_cu_comenzi", x => x.id_produse_cu_comenzi);
                    table.ForeignKey(
                        name: "FK_Comenzi_PC",
                        column: x => x.id_comanda,
                        principalTable: "comenzi",
                        principalColumn: "id_comanda",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Culoare_ProduseComenzoi",
                        column: x => x.id_culoare,
                        principalTable: "culori",
                        principalColumn: "id_culoare",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Dimensiune_ProduseComenzi",
                        column: x => x.id_dimensiune,
                        principalTable: "dimensiuni",
                        principalColumn: "id_dimensiune");
                    table.ForeignKey(
                        name: "FK_Manopera_ProduseComenzi",
                        column: x => x.id_manopera,
                        principalTable: "manopere",
                        principalColumn: "id_manopera");
                    table.ForeignKey(
                        name: "FK_Produse_PC",
                        column: x => x.id_produs,
                        principalTable: "produse",
                        principalColumn: "id_produs",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Produse_Set",
                        column: x => x.id_set,
                        principalTable: "seturi",
                        principalColumn: "id_set",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "imagini",
                columns: table => new
                {
                    id_imagine = table.Column<int>(type: "int(1)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    cale_imagine = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    bucket_directory = table.Column<string>(type: "varchar(70)", maxLength: 70, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_produs_culoare = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imagini", x => x.id_imagine);
                    table.ForeignKey(
                        name: "FK_ProduseCuCuloare_Imagini",
                        column: x => x.id_produs_culoare,
                        principalTable: "culori_produse",
                        principalColumn: "id_produs_culoare",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_adrese_id_cont",
                table: "adrese",
                column: "id_cont");

            migrationBuilder.CreateIndex(
                name: "IX_adrese_id_detaliu_factura",
                table: "adrese",
                column: "id_detaliu_factura");

            migrationBuilder.CreateIndex(
                name: "IX_adrese_id_locatie",
                table: "adrese",
                column: "id_locatie");

            migrationBuilder.CreateIndex(
                name: "IX_asociere_seturi_id_culoare",
                table: "asociere_seturi",
                column: "id_culoare");

            migrationBuilder.CreateIndex(
                name: "IX_asociere_seturi_id_dimensiune",
                table: "asociere_seturi",
                column: "id_dimensiune");

            migrationBuilder.CreateIndex(
                name: "IX_asociere_seturi_id_manopera",
                table: "asociere_seturi",
                column: "id_manopera");

            migrationBuilder.CreateIndex(
                name: "IX_asociere_seturi_id_produs",
                table: "asociere_seturi",
                column: "id_produs");

            migrationBuilder.CreateIndex(
                name: "IX_asociere_seturi_id_set",
                table: "asociere_seturi",
                column: "id_set");

            migrationBuilder.CreateIndex(
                name: "IX_comenzi_id_adresa_facturare",
                table: "comenzi",
                column: "id_adresa_facturare");

            migrationBuilder.CreateIndex(
                name: "IX_comenzi_id_adresa_livrare",
                table: "comenzi",
                column: "id_adresa_livrare");

            migrationBuilder.CreateIndex(
                name: "IX_comenzi_id_voucher",
                table: "comenzi",
                column: "id_voucher");

            migrationBuilder.CreateIndex(
                name: "INDEX_EMAIL",
                table: "conturi",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cos_cumparaturi_id_cont",
                table: "cos_cumparaturi",
                column: "id_cont");

            migrationBuilder.CreateIndex(
                name: "IX_cos_cumparaturi_id_culoare",
                table: "cos_cumparaturi",
                column: "id_culoare");

            migrationBuilder.CreateIndex(
                name: "IX_cos_cumparaturi_id_dimensiune",
                table: "cos_cumparaturi",
                column: "id_dimensiune");

            migrationBuilder.CreateIndex(
                name: "IX_cos_cumparaturi_id_manopera",
                table: "cos_cumparaturi",
                column: "id_manopera");

            migrationBuilder.CreateIndex(
                name: "IX_cos_cumparaturi_id_produs",
                table: "cos_cumparaturi",
                column: "id_produs");

            migrationBuilder.CreateIndex(
                name: "IX_cos_cumparaturi_id_set",
                table: "cos_cumparaturi",
                column: "id_set");

            migrationBuilder.CreateIndex(
                name: "IX_culori_id_cod_culoare",
                table: "culori",
                column: "id_cod_culoare");

            migrationBuilder.CreateIndex(
                name: "IX_culori_produse_id_culoare",
                table: "culori_produse",
                column: "id_culoare");

            migrationBuilder.CreateIndex(
                name: "IX_culori_produse_id_produs",
                table: "culori_produse",
                column: "id_produs");

            migrationBuilder.CreateIndex(
                name: "IX_dimensiuni_produse_id_dimensiune",
                table: "dimensiuni_produse",
                column: "id_dimensiune");

            migrationBuilder.CreateIndex(
                name: "IX_dimensiuni_produse_id_produs",
                table: "dimensiuni_produse",
                column: "id_produs");

            migrationBuilder.CreateIndex(
                name: "IX_imagini_id_produs_culoare",
                table: "imagini",
                column: "id_produs_culoare");

            migrationBuilder.CreateIndex(
                name: "IX_manopere_id_inel_prindere",
                table: "manopere",
                column: "id_inel_prindere");

            migrationBuilder.CreateIndex(
                name: "IX_manopere_id_tip_galerie",
                table: "manopere",
                column: "id_tip_galerie");

            migrationBuilder.CreateIndex(
                name: "IX_manopere_id_tip_linie",
                table: "manopere",
                column: "id_tip_linie");

            migrationBuilder.CreateIndex(
                name: "Index_CodProdus",
                table: "produse",
                column: "cod_produs",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_produse_id_producator",
                table: "produse",
                column: "id_producator");

            migrationBuilder.CreateIndex(
                name: "IX_produse_cu_comenzi_id_comanda",
                table: "produse_cu_comenzi",
                column: "id_comanda");

            migrationBuilder.CreateIndex(
                name: "IX_produse_cu_comenzi_id_culoare",
                table: "produse_cu_comenzi",
                column: "id_culoare");

            migrationBuilder.CreateIndex(
                name: "IX_produse_cu_comenzi_id_dimensiune",
                table: "produse_cu_comenzi",
                column: "id_dimensiune");

            migrationBuilder.CreateIndex(
                name: "IX_produse_cu_comenzi_id_manopera",
                table: "produse_cu_comenzi",
                column: "id_manopera");

            migrationBuilder.CreateIndex(
                name: "IX_produse_cu_comenzi_id_produs",
                table: "produse_cu_comenzi",
                column: "id_produs");

            migrationBuilder.CreateIndex(
                name: "IX_produse_cu_comenzi_id_set",
                table: "produse_cu_comenzi",
                column: "id_set");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_id_cont",
                table: "reviews",
                column: "id_cont");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_id_produs",
                table: "reviews",
                column: "id_produs");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_id_set",
                table: "reviews",
                column: "id_set");

            migrationBuilder.CreateIndex(
                name: "IX_sesiuni_id_cont",
                table: "sesiuni",
                column: "id_cont",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tipuri_pe_produse_id_produs",
                table: "tipuri_pe_produse",
                column: "id_produs");

            migrationBuilder.CreateIndex(
                name: "IX_tipuri_pe_produse_id_tip_produs",
                table: "tipuri_pe_produse",
                column: "id_tip_produs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asociere_seturi");

            migrationBuilder.DropTable(
                name: "cos_cumparaturi");

            migrationBuilder.DropTable(
                name: "dimensiuni_produse");

            migrationBuilder.DropTable(
                name: "global_config");

            migrationBuilder.DropTable(
                name: "imagini");

            migrationBuilder.DropTable(
                name: "produse_cu_comenzi");

            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropTable(
                name: "sesiuni");

            migrationBuilder.DropTable(
                name: "tipuri_pe_produse");

            migrationBuilder.DropTable(
                name: "culori_produse");

            migrationBuilder.DropTable(
                name: "comenzi");

            migrationBuilder.DropTable(
                name: "dimensiuni");

            migrationBuilder.DropTable(
                name: "manopere");

            migrationBuilder.DropTable(
                name: "seturi");

            migrationBuilder.DropTable(
                name: "tipuri_produse");

            migrationBuilder.DropTable(
                name: "culori");

            migrationBuilder.DropTable(
                name: "produse");

            migrationBuilder.DropTable(
                name: "adrese");

            migrationBuilder.DropTable(
                name: "vouchere");

            migrationBuilder.DropTable(
                name: "inele_prindere");

            migrationBuilder.DropTable(
                name: "tipuri_galerie");

            migrationBuilder.DropTable(
                name: "tipuri_linie");

            migrationBuilder.DropTable(
                name: "cod_culori");

            migrationBuilder.DropTable(
                name: "producatori");

            migrationBuilder.DropTable(
                name: "conturi");

            migrationBuilder.DropTable(
                name: "detalii_factura");

            migrationBuilder.DropTable(
                name: "locatii");
        }
    }
}
