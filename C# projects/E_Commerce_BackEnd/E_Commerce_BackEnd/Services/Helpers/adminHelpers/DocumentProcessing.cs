using System.Globalization;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;
using E_Commerce_BackEnd.Services.uProductsService;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;


namespace E_Commerce_BackEnd.Services.Helpers.adminHelpers;
using GemBox.Spreadsheet;


public class DocumentProcessing
{
    private readonly string _freeKey = "FREE-LIMITED-KEY";
    private readonly ILogger<DocumentProcessing> _docsLogger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductService _productService;

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
        SpreadsheetInfo.SetLicense(_freeKey);
        var producatoriRepository = _unitOfWork.Repository<Producatori>();
        var culoriRepository = _unitOfWork.Repository<Culori>();
        var codCuloriRepository = _unitOfWork.Repository<CodCulori>();
        var tipuriProduseRepository = _unitOfWork.Repository<TipuriProduse>();
        
        var workbook = ExcelFile.Load(stream);
        IDbContextTransaction? dbContextTransaction = null;
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

                    var idProducator = 0;
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
                    // images processing
                    var relativePathOfImagesString = row.Cells[15].StringValue;
                    string[] relativePathOfImagesArray = [];

                    var checkEqualWithOrEmpty = string.Equals(folderName, "-") && string.IsNullOrEmpty(folderName);
                    
                    if(relativePathOfImagesArray.Length != 0 && checkEqualWithOrEmpty)
                    {
                        throw new Exception($"Nu ati selectat niciun fisier in care sa puneti imaginea " +
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
                        ActivInMagazinDto = false,
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
                        pretProdusArray, idProducator, tipProdus, folderName);

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
}
    