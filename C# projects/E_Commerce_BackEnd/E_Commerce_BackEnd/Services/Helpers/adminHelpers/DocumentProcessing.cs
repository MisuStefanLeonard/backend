using System.Globalization;
using E_Commerce_BackEnd.Models.ConfigurationModels;
using E_Commerce_BackEnd.Models.DTO;
using E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.ProductBillingDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ManopereDto.ManoperaForSet;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage.OptionsForCurtain;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ShoppingCartDtos;
using E_Commerce_BackEnd.Models.Enums;
using E_Commerce_BackEnd.Models.OrderRelatedModels;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;
using E_Commerce_BackEnd.Services.uProductsService;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using GemBox.Spreadsheet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;

namespace E_Commerce_BackEnd.Services.Helpers.adminHelpers;


public class DocumentProcessing
{
    private const string FreeKey = "FREE-LIMITED-KEY";
    private static readonly List<string> InvoiceCredentials = ["smart_bill_username", "smart_bill_password" , "cif"];
    private readonly ILogger<DocumentProcessing> _docsLogger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductService _productService;

    private static readonly IDictionary<string, string> CellsMapping = new Dictionary<string, string>
    {
        { "A1", "CodProdus" },
        { "B1" , "Descriere (Romana)"},
        { "C1" , "Nume Produs"},
        { "D1" , "Compozitie (Romana)"},
        { "E1" , "Pret"},
        { "F1" , "Tva"},
        { "G1" , "Ingrijire (Romana)"},
        { "H1" , "Fata reversibila"},
        { "I1" , "Stoc"},
        { "J1" , "Nume producator"},
        { "K1" , "Dimensiuni"},
        { "L1" , "Recomandare pat"},
        { "M1" , "Cod Culori"},
        { "N1" , "Categorii"},
        { "O1" , "Tip produs (Romana-Engleza)"},
        { "P1" , "Imagini"},
        { "Q1" , "Numele folderului de stocare"},
        { "R1" , "Pret baza produs"},
        { "S1" , "Pret baza produs redus"},
        { "T1" , "Inaltime maxima material (metri)"},
        { "U1" , "Descriere (Engleza)"},
        { "V1" , "Compozitie (Engleza)"},
        { "W1" , "Ingrijire (Engleza)"},
        { "X1" , "Activ in magazin"},

    };

    public DocumentProcessing(ILogger<DocumentProcessing> docsLogger, IUnitOfWork unitOfWork, IProductService productService)
    {
        _docsLogger = docsLogger;
        _unitOfWork = unitOfWork;
        _productService = productService;
        
    }

    private async Task ProcessDimensions(string[] dimensions, string[]? recomandari , IList<int> dimensionsIdList)
    {
        for (var i = 0; i < dimensions.Length; i++)
        {
            var dimensiuniRepository = _unitOfWork.Repository<Dimensiuni>();
            var currentDimensions = dimensions[i].Split("x");
            var latime = currentDimensions[0].Trim();
            var lungime = currentDimensions[1].Trim();
            var recomandare = recomandari?[i];

            var isDimensionInDatabase = await dimensiuniRepository
                .FindQueryable(d => d.Latime == latime && d.Lungime == lungime && d.RecomandarePat == recomandare)
                .FirstOrDefaultAsync();

            if (isDimensionInDatabase is null)
            {
                var newDimension = new Dimensiuni(lungime, latime, recomandare, null);
                await dimensiuniRepository.AddAsync(newDimension);
                await _unitOfWork.CommitAsync();
                isDimensionInDatabase = newDimension;
                _docsLogger.LogInformation($"Dimensiune({latime}x{lungime}) saved successfully");
                _docsLogger.LogInformation(recomandare != null
                    ? $"{recomandare} was linked with {latime}x{lungime} dimension"
                    : "No recomandare pat was linked with this dimension");
            }
            dimensionsIdList.Add(isDimensionInDatabase.IdDimensiune);
        }
    }
    
    
    private static bool IsRowEmpty(ExcelRow row)
    {
        // check all the cells
        foreach (var cell in row.AllocatedCells)
        {
            // if we find something return false
           
            if (cell.Value is not null)
            {
                return false; 
            }
        }
        return true; 
    }
    
    public async Task<ExcelProcessingResult> ReadProductsExcel(Stream stream)
    {
        SpreadsheetInfo.SetLicense(FreeKey);
        var producatoriRepository = _unitOfWork.Repository<Producatori>();
        var culoriRepository = _unitOfWork.Repository<Culori>();
        var codCuloriRepository = _unitOfWork.Repository<CodCulori>();
        var tipuriProduseRepository = _unitOfWork.Repository<TipuriProduse>();
        
        var workbook = ExcelFile.Load(stream);
        IDbContextTransaction? dbContextTransaction;
        dbContextTransaction  = await _unitOfWork.BeginTransactionAsync();
        try
        {

            foreach (var worksheet in workbook.Worksheets)
            {
                Console.WriteLine("Current worksheet -> " + worksheet.Name);

                for (var i = 1; i < worksheet.Rows.Count; i++)
                {
                    
                    IList<int> dimensionsIdList = [];
                    IList<int> tipuriProduseIdList = [];
                    IList<int> colorIdList = [];

                    int idProducator;
                    var row = worksheet.Rows[i];
                    _docsLogger.LogInformation($"Row : {row}");
                    
                    if (IsRowEmpty(row))
                    {
                        _docsLogger.LogInformation($"Skipping empty row {i + 1}");
                        continue;
                    }
                    var codProdus = row.Cells[0].StringValue.Trim().ToUpper();
                    if (string.IsNullOrEmpty(codProdus))
                    {
                        throw new Exception($"Codul produsului nu poate fi gol. Vezi linia {row.Name} in fisierul excel.");
                    }

                    var tipProdus = row.Cells[14].StringValue.ToLower().Trim();
                    
                    if (tipProdus.IsNullOrEmpty() || !tipProdus.Contains('-'))
                    {
                        throw new Exception($"Tipul produsului trebuie sa fie in formatul Romana-Engleza .Vezi randul {row.Name} coloana {i-1} in excel ");
                    }
                    
                    var tipProdusParts = tipProdus.Split('-');
                    
                    var descriereProdus = row.Cells[1].StringValue.Trim();
                    var descriereProdusEn = row.Cells[20].StringValue.Trim();
                    
                    var numeProdus = row.Cells[2].StringValue.Trim();
                    
                    if (numeProdus.IsNullOrEmpty())
                    {
                        throw new Exception(
                            $"Numele produsului nu poate fi gol! Vezi linia {row.Name} in fisierul excel.");
                    }

                    if (!numeProdus.Contains('-'))
                    {
                        throw new Exception(
                            $"Numele produsului trebuie sa fie in forma Denumire_Romana-Denumire_Engleza ! Vezi linia {row.Name} in fisierul excel.");
                    }

                    var numeProdusParts = numeProdus.Split('-');
                    var numeProdusRo = numeProdusParts[0].Trim();
                    var numeProdusEn = numeProdusParts[1].Trim();
                    
                    var compozitieProdus = row.Cells[3].StringValue.Trim();
                    var compozitieProdusEn = row.Cells[21].StringValue.Trim();
                    // processing the prices
                    var pretProdusString = row.Cells[4].StringValue.Trim();
                    string[] pretProdusArray = [];
                    if (!string.Equals(pretProdusString, "-") && !string.IsNullOrEmpty(pretProdusString))
                    {
                        pretProdusArray = pretProdusString.Split(","); // will be sent !
                    }

                    // ----
                    var tvaProdusString = row.Cells[5].StringValue;
                    if (byte.TryParse(tvaProdusString, out var tvaProdus))
                    {
                        _docsLogger.LogTrace("Just parsed the TVA succesfully");
                    }
                    else
                    {
                        throw new Exception("TVA could not be parsed! Invalid format");
                    }

                    //--
                    var ingrijireProdus = row.Cells[6].StringValue.Trim();
                    var ingrijireProdusEn = row.Cells[22].StringValue.Trim();
                    var isActiveInShop = row.Cells[23].StringValue.Trim();
                    var isActiveInShopBool = false;
                    if (!string.IsNullOrEmpty(isActiveInShop))
                    {
                        isActiveInShopBool = isActiveInShop == "1";
                    }
                    
                    // fata reversibila
                    var fataReversibila = row.Cells[7].StringValue.ToUpper().Trim() == "TRUE";
                  
                    // stoc produs
                    var stocProdusString = row.Cells[8].StringValue.Trim();
                    ushort stocProdus = 0;
                    if (!string.Equals(stocProdusString, "-") && !string.IsNullOrEmpty(stocProdusString))
                    {
                        stocProdus = ushort.Parse(stocProdusString);
                    }

                    var numeProducator = row.Cells[9].StringValue.ToUpper().Trim();

                    // producatorul ( Reig Marti)
                    if (!string.Equals(numeProducator, "-") && !string.IsNullOrEmpty(numeProducator))
                    {
                        var isProducatorInDb = await producatoriRepository
                            .FindQueryable(producatori => producatori.NumeProducator == numeProducator)
                            .FirstOrDefaultAsync();

                        if (isProducatorInDb is null)
                        {
                            var newProducator = new Producatori
                            (
                                numeProducator,
                                new HashSet<Produse>()
                            );

                            await producatoriRepository.AddAsync(newProducator);
                            await _unitOfWork.CommitAsync();

                            isProducatorInDb = newProducator;
                          
                            idProducator = isProducatorInDb.IdProducator;
                          
                        }
                        else
                        {
                            idProducator = isProducatorInDb.IdProducator;
                        }
                    }
                    else
                    {
                        _docsLogger.LogInformation("No producator was choosen. Empty id in produse table");
                        idProducator = 0;
                    }

                    // Recomandare pat processing and  DIMENSION PROCESSING
                    var dimensiuniFromExcel = row.Cells[10].StringValue.Trim();
                    var recomandarePatFromExcelRow = row.Cells[11].StringValue.Trim();

                    string[] recomandarePatArrayValues = [];
                    if (!recomandarePatFromExcelRow.IsNullOrEmpty() && !string.Equals(recomandarePatFromExcelRow, "-"))
                    {
                        recomandarePatArrayValues = recomandarePatFromExcelRow.Split(",");
                    }
                    var dimensiuniArray = dimensiuniFromExcel.Split(",");

                  

                    if (string.IsNullOrEmpty(recomandarePatFromExcelRow) || recomandarePatFromExcelRow == "-")
                    {
                        if (!string.IsNullOrEmpty(dimensiuniFromExcel) && dimensiuniFromExcel != "-")
                        {
                            await ProcessDimensions(dimensiuniArray, null, dimensionsIdList);
                        }
                        else
                        {
                            _docsLogger.LogInformation("Nicio dimensiune pentru produsul curent!_____1");
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(dimensiuniFromExcel) && dimensiuniFromExcel != "-")
                        {
                            if (dimensiuniArray.Length == recomandarePatArrayValues.Length)
                            {
                                await ProcessDimensions(dimensiuniArray, recomandarePatArrayValues,
                                    dimensionsIdList);
                            }
                            else
                            {
                                _docsLogger.LogError("Mismatch between dimensions and recomandare pat arrays.");
                                throw new Exception("Diferenta intre dimensiuni si recomandare pat(trebuie sa coincida 1:1 (dimensiune:recomandare_pat))." +
                                                    $"Vezi linia {row.Name} in excel");
                            }
                        }
                        else
                        {
                            _docsLogger.LogInformation("Nicio dimensiune pentru produsul curent!______2");
                        }
                    }
                    
                    var lenOfPrices = pretProdusArray.Length;
                    var lenOfDimensions = dimensionsIdList.Count;
                    var lenOfRecomandarePat = recomandarePatArrayValues.Length;

                    if (lenOfPrices != lenOfDimensions && lenOfPrices != lenOfRecomandarePat)
                    {
                        _docsLogger.LogError(
                            "Prices , dimensions and recomandare pat values are not equal in length");
                        throw new Exception(
                            "Preturile , dimensiunile si recomandarile de pat nu sunt de acceasi dimensiune!" +
                            $"Vezi randul {row.Name} in excel.");
                    }
                    

                    // PROCESAREA CULORILOR DIN EXCEL
                    // initial -> grey-03,blue-08
                    var culoriString = row.Cells[12].StringValue.ToLower().Trim();

                    if (string.Equals(culoriString, "-") && string.IsNullOrEmpty(culoriString))
                    {
                        _docsLogger.LogError("EROARE! Sectiunea de culori nu poate fi goala! ");
                        throw new Exception("EROARE! Sectiunea de culori nu poate fi goala! Vezi" +
                                            $"randul {i-1} pe coloana 13");
                    }

                    var culoriArray = culoriString.Split(","); // will be parsed !

                    foreach (var culoriPair in culoriArray)
                    {
                        var currentPair = culoriPair.Split("-");
                        var numeCuloare = currentPair[0].Trim().ToLower();
                        var codCuloare = currentPair[1].Trim();
                        var numeCuloareEn = currentPair[2].Trim().ToLower();
                        
                        _docsLogger.LogInformation($"numeculoare -> {numeCuloare}");
                        _docsLogger.LogInformation($"codCuloare -> {codCuloare}");

                        
                        var isCodCuloareInDb = await codCuloriRepository
                            .FindQueryable(cc => cc.CodCuloare == codCuloare)
                            .FirstOrDefaultAsync();

                        if (isCodCuloareInDb is null)
                        {
                            var newCodCuloare = new CodCulori
                            {
                                CodCuloare = codCuloare
                            };

                            await codCuloriRepository.AddAsync(newCodCuloare);
                            await _unitOfWork.CommitAsync();
                            isCodCuloareInDb = newCodCuloare;
                            _docsLogger.LogInformation(
                                $"CodCuloare({isCodCuloareInDb.CodCuloare}) saved succesfully");
                        }
                        else
                        {
                            _docsLogger.LogInformation($"Cod culoare {codCuloare} already was in db");
                        }

                        // var isCuloareInDb = await culoriRepository
                        //     .FindQueryable(c =>
                        //         c.NumeCuloare == numeCuloare && c.IdCodCuloare == isCodCuloareInDb.IdCodCuloare)
                        //     .FirstOrDefaultAsync();
                        
                        var isCuloareInDb = await culoriRepository
                            .FindQueryable(c =>
                                EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(c.NumeCuloareJson , "$.culoare_ro")) == numeCuloare && 
                                EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(c.NumeCuloareJson , "$.culoare_en")) == numeCuloareEn &&
                                c.IdCodCuloare == isCodCuloareInDb.IdCodCuloare)
                            .FirstOrDefaultAsync();

                        if (isCuloareInDb is null)
                        {
                            var newCuloare = new Culori
                            {
                                // NumeCuloare = numeCuloare.ToLower(),
                                NumeCuloareJson = new Culoare
                                {
                                    CuloareRomana = numeCuloare,
                                    CuloareEngleza = numeCuloareEn
                                },
                                IdCodCuloare = isCodCuloareInDb.IdCodCuloare
                            };
                            await culoriRepository.AddAsync(newCuloare);
                            await _unitOfWork.CommitAsync();
                            isCuloareInDb = newCuloare;
                            _docsLogger.LogInformation(
                                $"Culoare({isCuloareInDb.NumeCuloareJson.CuloareRomana} - cod(FK)-> {isCuloareInDb.IdCodCuloare}) saved succesfully");
                            colorIdList.Add(isCuloareInDb.IdCuloare);
                        }else
                        {
                            colorIdList.Add(isCuloareInDb.IdCuloare);
                            _docsLogger.LogInformation($"Culoare  {numeCuloare} already was in db");
                        }
                        

                    }

                    // processing the category of the products
                    var categoriiProdus = row.Cells[13].StringValue.ToUpper().Trim();

                    if (string.Equals(categoriiProdus, "-") && string.IsNullOrEmpty(categoriiProdus))
                    {
                        _docsLogger.LogError($"Categoria produsului nu poate fi goala! Vezi randul {row.Name} in excel coloana 14");
                        throw new Exception("Category cannot be null. Rolling back transactions");
                    }

                    var arrayCateogriiProdus = categoriiProdus.Split(","); // will be parsed!
                   
                    foreach (var categorie in arrayCateogriiProdus)
                    {
                        if (!categorie.Contains('-'))
                        {
                            _docsLogger.LogError($"Categoria produsului trebuie sa fie in formatul CATEGORIE_ROMANA-CATEGORIE_ENGLEZA! Vezi randul {row.Name} in excel coloana 14");
                            throw new Exception("Category format incorrect. Rolling back transactions");
                        }
                        
                        var parts = categorie.Split('-', 2); // Split into at most 2 parts
                        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
                        {
                            _docsLogger.LogError($"Categoria produsului trebuie sa fie in formatul CATEGORIE_ROMANA-CATEGORIE_ENGLEZA si nicio parte nu poate fi goala ! Vezi randul {row.Name} in excel coloana 14");
                            throw new Exception("Category format incorrect. Rolling back transactions");
                        }
                        var categoryRo = parts[0].ToUpper();
                        var categoryEn = parts[1].ToUpper();

                        var isCategorieInDatabase = await tipuriProduseRepository
                            .FindQueryable(tp =>   EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(tp.CategorieJson , "$.categorie_ro")) == categoryRo &&
                                                   EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(tp.CategorieJson , "$.categorie_en")) == categoryEn)
                            .FirstOrDefaultAsync();

                        if (isCategorieInDatabase is null)
                        {

                            var newCategorie = new TipuriProduse
                            {
                                // Categorie = categorie,
                                CategorieJson = new Categorie
                                {
                                    CategorieRomana = categoryRo,
                                    CategorieEngleza = categoryEn
                                }
                            };
                            await tipuriProduseRepository.AddAsync(newCategorie);
                            await _unitOfWork.CommitAsync();
                            _docsLogger.LogInformation("Categorie (cuvertura/perdea) added in the database");
                            isCategorieInDatabase = newCategorie;

                        }

                        tipuriProduseIdList.Add(isCategorieInDatabase.IdTipProdus);
                    }
                    
                    var folderName = row.Cells[16].StringValue.ToLower().Trim();
                    var folderNamesArray = folderName.Split(",");
                    // images processing
                    var relativePathOfImagesString = row.Cells[15].StringValue;
                    string[] relativePathOfImagesArray = [];

                    var checkEqualWithOrEmpty = string.Equals(folderName, "-") && string.IsNullOrEmpty(folderName);
                    
                    if(relativePathOfImagesArray.Length != 0 && checkEqualWithOrEmpty)
                    {
                        throw new Exception($"Nu ati selectat niciun fisier in care sa puneti imaginea " +
                                            $"Va rog, selectati un fisier .Vezi randul {row.Name} in excel ");
                    }

                    if (relativePathOfImagesArray.Length != folderNamesArray.Length)
                    {
                        throw new Exception($"Acelasi numar de imagini trebuie sa fie egal cu numarul de fisiere " +
                                            $"Va rog, selectati un fisier .Vezi randul {row.Name} in excel ");
                    }
                    
                    
                    
                    if (!string.Equals(relativePathOfImagesString, "-") &&
                        !string.IsNullOrEmpty(relativePathOfImagesString))
                    {
                        relativePathOfImagesArray = relativePathOfImagesString.Split(',');
                    }
                    
                    
                    var productBasePrice = row.Cells[17].StringValue.Trim(); // case we have a perdea/draperie
                    decimal productBasePriceDecimal = 0;
                    var isDigits = productBasePrice.All(char.IsDigit);
                    if (isDigits)
                    {
                        productBasePriceDecimal = decimal.Parse(productBasePrice);
                    }
                    
                    var productBasePriceDiscounted = row.Cells[18].StringValue.Trim(); // case we have a perdea/draperie
                    decimal productBasePriceDiscountedDecimal = 0;
                    var isDigitsDiscounted = productBasePriceDiscounted.All(char.IsDigit);
                    if (isDigitsDiscounted)
                    {
                        productBasePriceDiscountedDecimal = decimal.Parse(productBasePriceDiscounted);
                    }

                    var maxMaterialHeight = row.Cells[19].StringValue.Trim();
                    decimal decMaxMaterialHeight = 0;
                    if (!string.Equals("-", maxMaterialHeight))
                    {
                        decMaxMaterialHeight = decimal.Parse(maxMaterialHeight , CultureInfo.InvariantCulture);
                    }

                    var newProdusDto = new ProduseDto
                    {
                        CodProdusDto = codProdus,
                        DescriereDto = descriereProdus,
                        DescriereJsonDto = new Descriere
                        {
                            DescriereRomana = descriereProdus,
                            DescriereEngleza = descriereProdusEn
                        }, // de completat
                        NumeProdusDto = numeProdus,
                        NumeProdusJsonDto = new Nume
                        {
                            NumeRomana = numeProdusRo,
                            NumeEngleza = numeProdusEn
                        },
                        CompozitieDto = compozitieProdus,
                        CompozitieJsonDto = new Compozitie
                        {
                            CompozitieRomana = compozitieProdus,
                            CompozitieEngleza = compozitieProdusEn
                        },
                        TvaDto = tvaProdus,
                        IngrijireDto = ingrijireProdus,
                        IngrijireJsonDto = new Ingrijire
                        {
                            IngrijireRomana = ingrijireProdus,
                            IngrijireEngleza = ingrijireProdusEn
                        },
                        PretBazaDto = productBasePriceDecimal,
                        PretBazaRedusDto = productBasePriceDiscountedDecimal,
                        FataReversibilaDto = fataReversibila,
                        StocDto = stocProdus,
                        IsDeletedDto = false,
                        ActivInMagazinDto = isActiveInShopBool,
                        TipProdusDto = tipProdus,
                        TipProdusJsonDto = new TipProdus
                        {
                            TipProdusRomana = tipProdusParts[0].ToLower(),
                            TipProdusEngleza = tipProdusParts[1].ToLower()
                        },
                        ProdusLimitatDto = false,
                        ActiveazaInNoutati = false,
                        InaltimeMaximaDto = decMaxMaterialHeight
                    };
                    
                    var responseAddProduct = await _productService.AddOrEditProductFromExcel(newProdusDto, dimensionsIdList,
                        relativePathOfImagesArray, tipuriProduseIdList, colorIdList,
                        pretProdusArray, idProducator, tipProdus, folderNamesArray);

                    switch (responseAddProduct)
                    {
                        case 1:
                            _docsLogger.LogInformation($"Product with id: {codProdus} was succesfully added");
                            break;
                        case 2:
                            _docsLogger.LogInformation($"Product with id: {codProdus} has been updated");
                            break;
                    }
                    
                }
            }

            await _unitOfWork.CommitTransactionAsync(dbContextTransaction);
            return ExcelProcessingResult.SuccessResult("Products were successfully added from the Excel file.");
        }
        catch (Exception e)
        { 
            await _unitOfWork.RollBackTransactionAsync(dbContextTransaction);
            return ExcelProcessingResult.ErrorResult($"Error: {e.Message}");
        }
    }

    public async Task<KeyValuePair<int , Stream?>> ExportProductsExcel()
    {
        IDbContextTransaction? exportExcelTransaction = null;
        try
        {
            SpreadsheetInfo.SetLicense("FREE-LIMITED-KEY");
            var workbook = new ExcelFile();
            const int maxRows = 150;
            var cellIndex = 2;
            var worksheetCount = 1;
            exportExcelTransaction = await _unitOfWork.BeginTransactionAsync();
            var getAllProductsInShop = await _unitOfWork.Repository<Produse>()
                .GetSimpleQueryable()
                .Include(manufacturers => manufacturers.Producator)
                .Include(productWithColors => productWithColors.PProduseCuCulori!)
                    .ThenInclude(colors => colors.Culoare)
                        .ThenInclude(colorsCodes => colorsCodes.CodCuloare)
                .Include(productWithColors => productWithColors.PProduseCuCulori!)
                    .ThenInclude(images => images.ImagProduseCuCulori)
                .Include(productDimensions => productDimensions.PProduseCuDimensiuni!)
                    .ThenInclude(dimensions => dimensions.PdDimensiune)
                .Include(productCategories => productCategories.PTipuriPeProduse!)
                    .ThenInclude(categories => categories.TppTipProdus)
                .ToListAsync();
            
            var currentWorksheet = workbook.Worksheets.Add($"Sheet{worksheetCount}");
            foreach (var cell in CellsMapping)
            {
                var excelCell = currentWorksheet.Cells[cell.Key];
                excelCell.Value = cell.Value;
            }

            if (getAllProductsInShop.Count == 0)
            {
                return new KeyValuePair<int, Stream?>(0, null); // no products in db
            }

            foreach (var product in getAllProductsInShop)
            {
                if (cellIndex <= maxRows-1)
                {
                    //
                    var productCodeCell = currentWorksheet.Cells[$"A{cellIndex}"];
                    productCodeCell.Value = product.CodProdus;
                   
                    // ro description , cell B
                    var productDescrptionCell = currentWorksheet.Cells[$"B{cellIndex}"];
                    productDescrptionCell.Value = product.DescriereJson!.DescriereRomana;
                    // product name , cell C
                    var productNameCell = currentWorksheet.Cells[$"C{cellIndex}"];
                    productNameCell.Value = $"{product.NumeProdusJson.NumeRomana}-{product.NumeProdusJson.NumeEngleza}";
                    // product compozition , cell D
                    var productCompozition = currentWorksheet.Cells[$"D{cellIndex}"];
                    productCompozition.Value = product.CompozitieJson!.CompozitieRomana;
                    
                    // product price , cell E
                    var productPrices = currentWorksheet.Cells[$"E{cellIndex}"];
                    if (product.TipulProdusuluiJson.TipProdusRomana is "perdea" or "draperie")
                    {
                        productPrices.Value = "-";
                    }
                    if (product.PProduseCuDimensiuni!.Count > 0)
                    {
                        productPrices.Value = string.Join(",",
                            product.PProduseCuDimensiuni.Select(d => d.Pret.ToString("F2", CultureInfo.InvariantCulture)));
                    }
                    
                    // product TVA , cell F
                    var productTvaCell = currentWorksheet.Cells[$"F{cellIndex}"];
                    productTvaCell.Value = product.Tva;
                    // product RO caring , cell G
                    var productCaringCell = currentWorksheet.Cells[$"G{cellIndex}"];
                    productCaringCell.Value = product.IngrijireJson!.IngrijireRomana;
                    // product reverse face , cell H
                    var productReverseFaceCell =  currentWorksheet.Cells[$"H{cellIndex}"];
                    productReverseFaceCell.Value = product.FataReversibila!.Value;
                    // product stock , cell I
                    var productStockCell = currentWorksheet.Cells[$"I{cellIndex}"];
                    productStockCell.Value = product.Stoc == 0 ? "-" : product.Stoc;
                    // product manufacturer , cell J
                    var productManufacturerCell = currentWorksheet.Cells[$"J{cellIndex}"];
                    productManufacturerCell.Value =
                        product.Producator != null ? product.Producator.NumeProducator : "-";
                    
                    // product dimensions , cell K
                    var productDimensionsCell = currentWorksheet.Cells[$"K{cellIndex}"];

                    if (product.PProduseCuDimensiuni.Count > 0)
                    {
                        productDimensionsCell.Value = string.Join(",",
                            product.PProduseCuDimensiuni.Select(d => $"{d.PdDimensiune!.Lungime}x{d.PdDimensiune.Latime}"));
                    }

                    if (product.TipulProdusuluiJson.TipProdusRomana is "perdea" or "draperie")
                    {
                        productDimensionsCell.Value = "-";
                    }
                    
                    // product bed recommendation , cell L
                    var productBedRecommendations = currentWorksheet.Cells[$"L{cellIndex}"];

                    if (product.PProduseCuDimensiuni.Count > 0)
                    {
                        productBedRecommendations.Value = string.Join(",",
                            product.PProduseCuDimensiuni.Select(d => d.PdDimensiune!.RecomandarePat));
                    }

                    if (product.TipulProdusuluiJson.TipProdusRomana is "perdea" or "draperie")
                    {
                        productBedRecommendations.Value = "-";
                    }
                    
                    // product color codes , cell M
                    var productColorCodesRecommendations = currentWorksheet.Cells[$"M{cellIndex}"];
                    productColorCodesRecommendations.Value = string.Join(",",
                        product.PProduseCuCulori!.Select(c => $"{c.Culoare.NumeCuloareJson.CuloareRomana}" +
                                                              $"-{c.Culoare.CodCuloare.CodCuloare}-{c.Culoare.NumeCuloareJson.CuloareEngleza}"));
                    
                    // product categories , cell N 
                    var productCategories = currentWorksheet.Cells[$"N{cellIndex}"];
                    productCategories.Value = string.Join(",",
                        product.PTipuriPeProduse!.Select(c => $"{c.TppTipProdus.CategorieJson.CategorieRomana}-{c.TppTipProdus.CategorieJson.CategorieEngleza}"));
                    
                    // product type . cell O
                    var productType = currentWorksheet.Cells[$"O{cellIndex}"];
                    productType.Value =
                        $"{product.TipulProdusuluiJson.TipProdusRomana}-{product.TipulProdusuluiJson.TipProdusEngleza}";
                    
                    // product images , cell P
                    
                    var productImages = currentWorksheet.Cells[$"P{cellIndex}"];
                    
                    var images = product.PProduseCuCulori?
                        .SelectMany(p => p.ImagProduseCuCulori!)
                        .Select(img => img.CaleImagine)
                        .ToList();

                    productImages.Value = images == null || images.Count == 0
                        ? "-"
                        : string.Join(",", images);
                    
                    // product images directories associated with each image , CELL Q
                    var productImagesDirectories = currentWorksheet.Cells[$"Q{cellIndex}"];

                    
                    var directories = product.PProduseCuCulori?
                        .SelectMany(p => p.ImagProduseCuCulori!)
                        .Select(img => img.FisierInBucket)
                        .ToList();

                    productImagesDirectories.Value = (directories == null || directories.Count == 0)
                        ? "-"
                        : string.Join(",", directories);
                    
                    // product base price , CELL R
                    var productBasePrice = currentWorksheet.Cells[$"R{cellIndex}"];

                    productBasePrice.Value = product.PProduseCuDimensiuni.Count > 0 ? "-" : product.PretDeBaza.ToString("F2",CultureInfo.InvariantCulture);
                    
                    // product base price discounted , CELL S
                    var productBasePriceDiscounted = currentWorksheet.Cells[$"S{cellIndex}"];

                    if (product.PProduseCuDimensiuni.Count > 0)
                    {
                        productBasePriceDiscounted.Value = "-";
                    }
                    else
                    {
                        productBasePriceDiscounted.Value = product.PretDeBazaRedus != 0 ? product.PretDeBazaRedus.ToString("F2",CultureInfo.InvariantCulture) : "-";
                    }
                    
                    // product max material height , CELL T
                    var productMaxMaterialHeight = currentWorksheet.Cells[$"T{cellIndex}"];
                    productMaxMaterialHeight.Value =
                        product.TipulProdusuluiJson.TipProdusRomana is "perdea" or "draperie"
                            ? product.InaltimeMaxima.ToString("F2",CultureInfo.InvariantCulture)
                            : "-";
                    
                    // product EN description , cell U
                    var productDescriptionEnCell = currentWorksheet.Cells[$"U{cellIndex}"];
                    productDescriptionEnCell.Value = product.DescriereJson!.DescriereEngleza;
                    // product EN composition , cell V
                    var productCompositionEnCell = currentWorksheet.Cells[$"V{cellIndex}"];
                    productCompositionEnCell.Value = product.CompozitieJson!.CompozitieEngleza;
                    // product EN caring , cell W
                    var productCaringEnCell = currentWorksheet.Cells[$"W{cellIndex}"];
                    productCaringEnCell.Value = product.IngrijireJson!.IngrijireEngleza;
                    // product activation in shop , cell X
                    var productActivationInShop =  currentWorksheet.Cells[$"X{cellIndex}"];
                    productActivationInShop.Value = product.ActivInMagazin ? "1" : "0";
                    
                    cellIndex++;
                }
                else
                {
                    worksheetCount++;
                    if (worksheetCount == 6)
                    {
                        // free limit exceeded.
                        break;
                    }
                    cellIndex = 2;
                    currentWorksheet = workbook.Worksheets.Add($"Sheet{worksheetCount}");
                    foreach (var cell in CellsMapping)
                    {
                        var excelCell = currentWorksheet.Cells[cell.Key];
                        excelCell.Style.Font.Name = "Helvetica Neue";
                        excelCell.Style.Font.Size = 10;
                        excelCell.Value = cell.Value;
                    }
                }
            }

            var isProduction = Environment.GetEnvironmentVariable("DOCKER");
            if (isProduction != "true")
            {
                workbook.Save($"Produse_{DateTime.Now.Date:yyyy-MM-dd}.xlsx");
            }
            var memoryStream = new MemoryStream();
            memoryStream.Position = 0;
            workbook.Save(memoryStream, new XlsxSaveOptions
            {
                ImageDpi = 330,
                
            });
            return new KeyValuePair<int, Stream?>(1 , memoryStream);

        }
        catch (Exception e)
        {
            if (exportExcelTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(exportExcelTransaction);
            }
            _docsLogger.LogError(e.Message);
            _docsLogger.LogError(e.StackTrace);
            
            return new KeyValuePair<int, Stream?>(-1 , null);

        }
    }

    public async Task<KeyValuePair<int,string>> GenerateBill(int orderId, string currency = "RON" )
    {
        try
        {
            _docsLogger.LogInformation("Generating bill...");
            const string invoiceApiEndpoint = "https://ws.smartbill.ro/SBORO/api/invoice";
            var getInvoiceCredentials = await _unitOfWork.Repository<GlobalConfigs>()
                .FindQueryable(setting => InvoiceCredentials.Contains(setting.NumeAtributGlobal))
                .ToListAsync();

            var username = getInvoiceCredentials.Find(p => p.NumeAtributGlobal == "smart_bill_username");
            var password = getInvoiceCredentials.Find(p => p.NumeAtributGlobal == "smart_bill_password");
            var cif = getInvoiceCredentials.Find(p => p.NumeAtributGlobal == "cif");

            if (username == null || password == null || cif == null ||
                username.NumeAtributGlobal.IsNullOrEmpty() ||
                password.NumeAtributGlobal.IsNullOrEmpty() ||
                cif.NumeAtributGlobal.IsNullOrEmpty())
            {
                return new KeyValuePair<int, string>(-2, "");
            }

            var getOrderDetails = await _unitOfWork.Repository<Comenzi>()
                .FindQueryable(order => order.IdComanda == orderId)
                .Select(group => new OrderDetailsForBillingDto
                {
                    OrderId = group.IdComanda,
                    OrderDate = group.DataEmitereComanda,
                    OrderName = group.NumePeComanda,
                    OrderPrename = group.PrenumePeComanda,
                    OrderPhoneNumber = group.NrTelefonPeComanda,
                    OrderBillNumber = group.BillNumberJson,
                    OrderEmail = group.EmailPeComanda,
                    Items = group.PcComenzi!
                        .GroupBy(groupItems => groupItems.IdentificatorSet != "21" ? groupItems.IdentificatorSet : groupItems.IdProduseCuComenzi.ToString())
                        .Select(item => new GroupedCartItems
                        {
                            Key = item.Key,
                            CartItems = item.Select(product => new CartItems
                            {
                                IdProdus = product.Produs.IdProdus,
                                IdSet = product.Set!.IdSet,
                                NumeSet =  currency == "RON" ? product.Set!.NumeSetJson.NumeRomana :  product.Set!.NumeSetJson.NumeEngleza,
                                CodProdus = product.Produs.CodProdus,
                                NumeProdus = currency == "RON" ?  product.Produs.NumeProdusJson.NumeRomana : product.Produs.NumeProdusJson.NumeEngleza ,
                                TipProdus = currency == "RON" ?  product.Produs.TipulProdusuluiJson.TipProdusRomana :  product.Produs.TipulProdusuluiJson.TipProdusEngleza,
                                TipProdusJson = product.Produs.TipulProdusuluiJson,
                                CuloareSelectata = new CuloriDto
                                {
                                    NumeCuloareDto = currency == "RON" ?  product.PcCuloare.NumeCuloareJson.CuloareRomana : product.PcCuloare.NumeCuloareJson.CuloareEngleza,
                                    CodCuloareDto = product.PcCuloare.CodCuloare.CodCuloare!,
                                    
                                },
                                DimensiuneSelectata = new DimensiuniDto
                                {
                                    LungimeDto = product.PcManopera!.NumeManoperaJson!.NumeRomana != "STAN" ?  product.PcDimensiune!.Lungime : ((int)(product.PcManopera.MaterialFolosit * 100)).ToString(),
                                    LatimeDto = product.PcDimensiune!.Lungime,
                                    RecomandarePat = product.PcDimensiune.RecomandarePat,
                                    PerdeaEstePerecheDto = product.PcManopera!.NumeManoperaJson.NumeRomana == "STAN" ? null : product.PcDimensiune.PerdeaEstePereche 
                                        
                                },
                                SelectedManopera = product.Produs.TipulProdusuluiJson.TipProdusRomana == "perdea" || product.Produs.TipulProdusuluiJson.TipProdusRomana == "draperie" ? new StandardManopereOnSet
                                {
                                    IdManopera = product.PcManopera!.IdManopera,
                                    NumeManopera = currency == "RON" ?  product.PcManopera.NumeManoperaJson!.NumeRomana : product.PcManopera.NumeManoperaJson!.NumeEngleza ,
                                    MetruTotalFolosit = product.PcManopera.MaterialFolosit,
                                    InaltimeMaxima = product.PcManopera.InaltimeMaxima,
                                    TipInel = product.PcManopera.InelPrindereLaManopera != null ? new TipIneleDto
                                    {
                                        IdInelPrindere = product.PcManopera.InelPrindereLaManopera.IdInel,
                                        NumeTipInel =  currency == "RON" ?  product.PcManopera.InelPrindereLaManopera.CuloareInelJson.CuloareRomana
                                            : product.PcManopera.InelPrindereLaManopera.CuloareInelJson.CuloareEngleza,
                                    } : null,
                                    TipGalerie = new TipRejansaDto
                                    {
                                        IdRejansa = product.PcManopera.TipGalerieLaManopera.IdTipGalerie,
                                        NumeTipRejansa =  currency == "RON" ?  product.PcManopera.TipGalerieLaManopera.NumeTipGalerieJson.NumeRomana
                                            :  product.PcManopera.TipGalerieLaManopera.NumeTipGalerieJson.NumeEngleza,
                                        IncretireRejansa = product.PcManopera.TipGalerieLaManopera.IncretireRejansa,
                                        SePrindeCuInele = product.PcManopera.TipGalerieLaManopera.SePrindeCuInele
                                    },
                                    TipLinie = new TipLinieDto
                                    {
                                        IdTipLinie = product.PcManopera.TipLinieLaManopera.IdTipLinie,
                                        NumeTipCusaturaColt =  currency == "RON" ?  product.PcManopera.TipLinieLaManopera.NumeTipLinieJson.NumeRomana
                                            : product.PcManopera.TipLinieLaManopera.NumeTipLinieJson.NumeEngleza,
                                    }
                                } : null,
                                LungimeCeruta = product.PcManopera != null ?
                                    product.PcManopera.NumeManoperaJson.NumeRomana == "STAN" ? product.PcManopera.MaterialFolosit.ToString() : "empty"
                                : "not_perdea",
                                InaltimeCeruta = product.IdSet != null ? product.InaltimeAleasaPentruSet : "not_set",
                                PretCurent = currency == "RON" ? product.PretCumparat : product.PretCumparat / 5 ,
                                Cantitate = product.NrBucati,
                                IdentificatorSet = item.Key  
                            }).ToList()
                        }).ToList(),
                    ClientDeliveryAddress = new AdreseDto
                    {
                        IdAdresa = group.CAdresaLivrare.IdAdresa,
                        AliasDto = group.CAdresaLivrare.Alias,
                        TipAdresaDto = TipAdrese.Livrare,
                        BlocDto = group.CAdresaLivrare.Bloc,
                        NrBlocDto = group.CAdresaLivrare.NrBloc,
                        StradaDto = group.CAdresaLivrare.Strada,
                        NrStradaDto = group.CAdresaLivrare.NrStrada,
                        OrasDto = group.CAdresaLivrare.Locatie.Oras!,
                        JudetDto = group.CAdresaLivrare.Locatie.Judet!,
                        CodPostalDto = group.CAdresaLivrare.Locatie.CodPostal!,
                        IsDeletedDto = group.CAdresaLivrare.IsDeleted,
                        CifDto = null,
                        NumeFirmaDto = null
                    },
                    ClientBillingAddress = new AdreseDto
                    {
                        IdAdresa = group.CAdresaFacturare.IdAdresa,
                        AliasDto = group.CAdresaFacturare.Alias,
                        TipAdresaDto = TipAdrese.Facturare,
                        BlocDto = group.CAdresaFacturare.Bloc,
                        NrBlocDto = group.CAdresaFacturare.NrBloc,
                        StradaDto = group.CAdresaFacturare.Strada,
                        NrStradaDto = group.CAdresaFacturare.NrStrada,
                        OrasDto = group.CAdresaFacturare.Locatie.Oras!,
                        JudetDto = group.CAdresaFacturare.Locatie.Judet!,
                        CodPostalDto = group.CAdresaFacturare.Locatie.CodPostal!,
                        CifDto = group.CAdresaFacturare.DetaliuFactura!.Cif,
                        NumeFirmaDto = group.CAdresaFacturare.DetaliuFactura!.Cif,
                    },
                })
                .AsSplitQuery()
                .FirstOrDefaultAsync();

            if (getOrderDetails == null )
            {
                return new KeyValuePair<int, string>(-3 , "Order not found");
            }
            

            if (currency == "RON")
            {
                if (getOrderDetails.OrderBillNumber is { NumarRomana: not null })
                {
                    return new KeyValuePair<int, string>(-4 , "Bill already generated on RON"); // bill on RON already generated
                }
            }
            else
            {
                if (getOrderDetails.OrderBillNumber is { NumarEngleza: not null })
                {
                    return new KeyValuePair<int, string>(-4 , "Bill already generated on EUR");
                }
            }
            

            var productsBillingList = new List<ProductBilling>();
            foreach (var product in getOrderDetails.Items)
            {
                var isNotSet = int.TryParse(product.Key, out _);
                _docsLogger.LogInformation($"is set: {isNotSet}");
                foreach (var item in product.CartItems)
                {
                    var descriptionCharacteristics = "";
                    switch (isNotSet)
                    {
                        // if we found the product!
                        case true:

                            if (item.TipProdusJson.TipProdusRomana is "perdea" or "draperie")
                            {
                                if (item.SelectedManopera!.NumeManopera is "STAN")
                                {
                                    descriptionCharacteristics += $"Material: {item.SelectedManopera.MetruTotalFolosit} m\n" +
                                                                  $"{(currency == "RON" ? "Stare material: neprocesat" : "Material state: unprocessed")}\n" +
                                                                  $"{(currency == "RON" ? $"Culoare: {item.CuloareSelectata.NumeCuloareDto}" : $"Color: {item.CuloareSelectata.NumeCuloareDto}")}";
                                }
                                else
                                {

                                    var ringTypeDescription = item.SelectedManopera.TipGalerie.SePrindeCuInele
                                        ? currency == "RON"
                                            ? $"Culoare inel: {item.SelectedManopera.TipInel!.NumeTipInel}"
                                            : $"Ring color: {item.SelectedManopera.TipInel!.NumeTipInel}"
                                        : "";


                                    descriptionCharacteristics +=
                                        $"{(currency == "RON" ? "Stare material: procesat" : "Material state: processed")}\n" +
                                        $"{(currency == "RON" ? $"Culoare: {item.CuloareSelectata.NumeCuloareDto}" : $"Color: {item.CuloareSelectata.NumeCuloareDto}")}\n" +
                                        $"Material: {item.SelectedManopera.MetruTotalFolosit} m\n" +
                                        $"{(currency == "RON" ? $"Dimensiuni: Lungime sina:{item.DimensiuneSelectata!.LungimeDto} cm , Inaltime sina:{item.DimensiuneSelectata.LatimeDto} cm"
                                            : $"Dimension: Rail width:{item.DimensiuneSelectata!.LungimeDto} cm , Rail height:{item.DimensiuneSelectata!.LatimeDto} cm")} \n" +
                                        $"{(currency == "RON" ? $"Rejansa: {item.SelectedManopera.TipGalerie.NumeTipRejansa}, Incretire: {item.SelectedManopera.TipGalerie.NumeTipRejansa} mm"
                                            : $"Rejuvenation type: {item.SelectedManopera.TipGalerie.NumeTipRejansa}, Pleat: {item.SelectedManopera.TipGalerie.NumeTipRejansa} mm")}\n" +
                                        $"{ringTypeDescription}\n" +
                                        $"{(currency == "RON" ? $"Tip cusatura colt: {item.SelectedManopera.TipLinie.NumeTipCusaturaColt}"
                                            : $"Cornet stitch type: {item.SelectedManopera.TipLinie.NumeTipCusaturaColt}")}\n";
                                }
                            }
                            else
                            {
                                var dimensionCharacteristic = item.DimensiuneSelectata != null
                                    ? $"{(currency == "RON" ? $"Dimensiuni: Lungime:{item.DimensiuneSelectata!.LungimeDto} cm , Latime:{item.DimensiuneSelectata.LatimeDto} cm " +
                                                              $", Recomandare pat: {item.DimensiuneSelectata.RecomandarePat ?? "-"}"
                                        : $"Dimension: Width:{item.DimensiuneSelectata!.LungimeDto} cm , Height:{item.DimensiuneSelectata.LatimeDto} cm" +
                                          $", Bed recommendation {item.DimensiuneSelectata.RecomandarePat ?? "-"}")} \n"
                                    : "";

                                descriptionCharacteristics +=
                                    $"{(currency == "RON" ? $"Culoare: {item.CuloareSelectata.NumeCuloareDto}" : $"Color: {item.CuloareSelectata.NumeCuloareDto}")}\n" +
                                    $"{dimensionCharacteristic}\n";
                            }


                            var newProductBilled = new ProductBilling
                            {
                                name = item.NumeProdus,
                                code = item.CodProdus,
                                productDescription = descriptionCharacteristics,
                                translatedName = currency == "EUR" ?  item.NumeProdus : "",
                                translatedMeasuringUnit = currency == "EUR" ? "piece" : "",
                                isDiscount = false,
                                measuringUnitName = currency == "RON" ? "buc" : "piece",
                                currency = currency,
                                quantity = item.Cantitate,
                                price = double.Parse(item.PretCurent.ToString()),
                                isTaxIncluded = true,
                                taxName = "Normala",
                                taxPercentage = 19,
                                isService = false,
                            };
                            productsBillingList.Add(newProductBilled);
                            break;
                        // else we found a set 
                        case false:
                            var descriptionCharacteristicsSet = $"{item.CodProdus}\n" +
                                                                $"{(currency == "RON" ? item.TipProdusJson.TipProdusRomana
                                                                    : item.TipProdusJson.TipProdusEngleza)}\n";
                            if (item.TipProdusJson.TipProdusRomana is "perdea" or "draperie")
                            {
                                if (item.SelectedManopera!.NumeManopera is "STAN")
                                {
                                    descriptionCharacteristicsSet += $"Material: {item.SelectedManopera.MetruTotalFolosit} m\n" +
                                                                     $"{(currency == "RON" ? "Stare material: neprocesat" : "Material state: unprocessed")}\n" +
                                                                     $"{(currency == "RON" ? $"Culoare: {item.CuloareSelectata.NumeCuloareDto}" : $"Color: {item.CuloareSelectata.NumeCuloareDto}")}";
                                }
                                else
                                {

                                    var ringTypeDescription = item.SelectedManopera.TipGalerie.SePrindeCuInele
                                        ? currency == "RON"
                                            ? $"Culoare inel: {item.SelectedManopera.TipInel!.NumeTipInel}"
                                            : $"Ring color: {item.SelectedManopera.TipInel!.NumeTipInel}"
                                        : "";


                                    descriptionCharacteristicsSet +=
                                        $"{(currency == "RON" ? "Stare material: procesat" : "Material state: processed")}\n" +
                                        $"{(currency == "RON" ? $"Culoare: {item.CuloareSelectata.NumeCuloareDto}" : $"Color: {item.CuloareSelectata.NumeCuloareDto}")}\n" +
                                        $"Material: {item.SelectedManopera.MetruTotalFolosit} m\n" +
                                        $"{(currency == "RON" ? $"Dimensiuni: Lungime sina:{item.DimensiuneSelectata!.LungimeDto} cm , Inaltime sina:{item.DimensiuneSelectata.LatimeDto} cm"
                                            : $"Dimension: Rail width:{item.DimensiuneSelectata!.LungimeDto} cm , Rail height:{item.DimensiuneSelectata!.LatimeDto} cm")} \n" +
                                        $"{(currency == "RON" ? $"Rejansa: {item.SelectedManopera.TipGalerie.NumeTipRejansa}, Incretire: {item.SelectedManopera.TipGalerie.NumeTipRejansa} mm"
                                            : $"Rejuvenation type: {item.SelectedManopera.TipGalerie.NumeTipRejansa}, Pleat: {item.SelectedManopera.TipGalerie.NumeTipRejansa} mm")}\n" +
                                        $"{ringTypeDescription}\n" +
                                        $"{(currency == "RON" ? $"Tip cusatura colt: {item.SelectedManopera.TipLinie.NumeTipCusaturaColt}"
                                            : $"Cornet stitch type: {item.SelectedManopera.TipLinie.NumeTipCusaturaColt}")}\n";
                                }
                            }
                            else
                            {
                                var dimensionCharacteristic = item.DimensiuneSelectata != null
                                    ? $"{(currency == "RON" ? $"Dimensiuni: Lungime:{item.DimensiuneSelectata!.LungimeDto} cm , Latime:{item.DimensiuneSelectata.LatimeDto} cm " +
                                                              $", Recomandare pat: {item.DimensiuneSelectata.RecomandarePat ?? "-"}"
                                        : $"Dimension: Width:{item.DimensiuneSelectata!.LungimeDto} cm , Height:{item.DimensiuneSelectata.LatimeDto} cm" +
                                          $", Bed recommendation {item.DimensiuneSelectata.RecomandarePat ?? "-"}")} \n"
                                    : "";

                                descriptionCharacteristicsSet +=
                                    $"{(currency == "RON" ? $"Culoare: {item.CuloareSelectata.NumeCuloareDto}" : $"Color: {item.CuloareSelectata.NumeCuloareDto}")}\n" +
                                    $"{dimensionCharacteristic}\n";
                            }


                            var findSetAlreadyInProductsToBeBilled = productsBillingList
                                .FirstOrDefault(p =>
                                    p.name == item.NumeSet);

                            if (findSetAlreadyInProductsToBeBilled == null)
                            {
                                var newSetBilled = new ProductBilling
                                {
                                    name = item.NumeSet!,
                                    code =item.NumeSet!,
                                    productDescription = descriptionCharacteristicsSet,
                                    translatedName = currency == "EUR" ? item.NumeSet! : "",
                                    translatedMeasuringUnit = currency == "EUR" ? "piece" : "",
                                    isDiscount = false,
                                    measuringUnitName = currency == "RON" ? "buc" : "piece",
                                    currency = currency,
                                    quantity = item.Cantitate,
                                    price = double.Parse(item.PretCurent.ToString()),
                                    isTaxIncluded = true,
                                    taxName = "Normala",
                                    taxPercentage = 19,
                                    isService = false,
                                };
                                productsBillingList.Add(newSetBilled);
                            }
                            else
                            {
                                findSetAlreadyInProductsToBeBilled.productDescription +=
                                    "\n" + descriptionCharacteristicsSet;
                            }

                            break;
                    }
                }
            }




            var jsonBody = new
            {
                companyVatCode = cif.ValoareAtributGlobal,
                client = new
                {
                    name = getOrderDetails.ClientBillingAddress!.IdAdresa != getOrderDetails.ClientDeliveryAddress!.IdAdresa ?
                        $"{getOrderDetails.ClientBillingAddress.NumeFirmaDto}" 
                        : $"{getOrderDetails.OrderName} {getOrderDetails.OrderPrename}",
                    vatCode =  getOrderDetails.ClientBillingAddress!.IdAdresa != getOrderDetails.ClientDeliveryAddress!.IdAdresa ?
                        $"{getOrderDetails.ClientBillingAddress.CifDto}"
                        : "",
                    address =  getOrderDetails.ClientBillingAddress!.IdAdresa != getOrderDetails.ClientDeliveryAddress!.IdAdresa ?
                        $"Str. {getOrderDetails.ClientBillingAddress.StradaDto}, nr. {getOrderDetails.ClientBillingAddress.NrStradaDto}" +
                        $", bl. {(getOrderDetails.ClientBillingAddress.BlocDto.IsNullOrEmpty() ? "-" : getOrderDetails.ClientBillingAddress.BlocDto)}," +
                        $" nr. {(getOrderDetails.ClientBillingAddress.NrBlocDto.IsNullOrEmpty() ? "-" : getOrderDetails.ClientBillingAddress.NrBlocDto)}" 
                        :     
                        $"Str. {getOrderDetails.ClientDeliveryAddress.StradaDto}, nr. {getOrderDetails.ClientDeliveryAddress.NrStradaDto}" +
                        $", bl. {(getOrderDetails.ClientDeliveryAddress.BlocDto.IsNullOrEmpty() ? "-" : getOrderDetails.ClientDeliveryAddress.BlocDto)}," +
                        $" nr. {(getOrderDetails.ClientDeliveryAddress.NrBlocDto.IsNullOrEmpty() ? "-" : getOrderDetails.ClientDeliveryAddress.NrBlocDto)}" ,
                    isTaxPayer = true,
                    // aici daca e Bucuresti , sectorul aferent adresei
                    city = getOrderDetails.ClientBillingAddress!.IdAdresa != getOrderDetails.ClientDeliveryAddress!.IdAdresa ?
                        getOrderDetails.ClientBillingAddress.OrasDto
                        : getOrderDetails.ClientDeliveryAddress.OrasDto,
                    // aici daca e Bucuresti , judetul Bucuresti
                    county =getOrderDetails.ClientBillingAddress!.IdAdresa != getOrderDetails.ClientDeliveryAddress!.IdAdresa ?
                        getOrderDetails.ClientBillingAddress.JudetDto 
                        : getOrderDetails.ClientDeliveryAddress.JudetDto,
                    country = "Romania",
                    phone = getOrderDetails.OrderPhoneNumber,
                    email = getOrderDetails.OrderEmail,
                    // saveToDb = true true to prod
                    saveToDb = false
                    
                },
                seriesName = "THD2015",
                isDraft = false,
                issueDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                currency = currency == "RON" ? "RON" : "EUR",
                exchangeRate = currency == "RON" ? double.Parse("1") : double.Parse("5") , 
                language = currency == "RON" ? "RO" : "EN",
                precision = 2,
                dueDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                deliveryDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                useEstimateDetails = false,
                products = productsBillingList
            };
            
            var serializedJson = JsonConvert.SerializeObject(jsonBody, Formatting.Indented);
            _docsLogger.LogInformation($"Content for bill generation \n{serializedJson}");
            var invoiceApiEndpointOptions = new RestClientOptions(invoiceApiEndpoint)
            {
                Authenticator = new HttpBasicAuthenticator(username.ValoareAtributGlobal , password.ValoareAtributGlobal)
            };
            var client = new RestClient(invoiceApiEndpointOptions);
            var request = new RestRequest
            {
                Method = Method.Post,
            };
            
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");
            request.AddJsonBody(serializedJson);
          
            var response = await client.ExecuteAsync(request);
            _docsLogger.LogInformation(response.Content);
            if (response.IsSuccessful)
            {
                _docsLogger.LogInformation($"Succefully generated bill for order with ID: {orderId}");
                JObject jsonResponse = JObject.Parse(response.Content!);
                var invoiceNumber = jsonResponse["number"]!.ToString();
                IDbContextTransaction? updateTransaction = null;
                try
                {
                    updateTransaction = await _unitOfWork.BeginTransactionAsync();
                    var getOrderToModify = await _unitOfWork.Repository<Comenzi>()
                        .FindQueryable(order => order.IdComanda == orderId)
                        .FirstAsync();

                    if (getOrderToModify.BillNumberJson is null)
                    {
                        getOrderToModify.BillNumberJson = new NumarFactura
                        {
                            NumarEngleza = null,
                            NumarRomana = null
                        };
                    }

                    if (currency == "RON")
                    {
                        getOrderToModify.BillNumberJson.NumarRomana = invoiceNumber;
                    }
                    else
                    {
                        getOrderToModify.BillNumberJson.NumarEngleza = invoiceNumber;

                    }
                    await _unitOfWork.Repository<Comenzi>().UpdateAsync(getOrderToModify);
                    await _unitOfWork.CommitTransactionAsync(updateTransaction);
                    return new KeyValuePair<int, string>(1 , $"{invoiceNumber}");
                }
                catch (Exception e)
                {
                    if (updateTransaction != null)
                    {
                        await _unitOfWork.RollBackTransactionAsync(updateTransaction);
                    }
                    _docsLogger.LogError("Tried to save the bill number, but did not succed. Error occured");
                    _docsLogger.LogError(e.Message);
                    return new KeyValuePair<int, string>(-1 , "");
                }
            }
            else
            {
                _docsLogger.LogInformation($"Unsuccefully generated bill for order with ID: {orderId}");
                _docsLogger.LogError(response.ErrorMessage);
                return new KeyValuePair<int, string>(-1 , "");

            }
        }
        catch (Exception e)
        {
            _docsLogger.LogError("Error when generating bill.");
            _docsLogger.LogError("Error message : {0}" , e.Message );
            _docsLogger.LogError(e.StackTrace);
            return new KeyValuePair<int, string>(-1 , "");

        }
    }
    
}
    