using E_Commerce_BackEnd.Models.DTO.ProduseDtos;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Services.uProductsService;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;

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
        for (int i = 0; i < dimensions.Length; i++)
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
                if (recomandare != null)
                {
                    _docsLogger.LogInformation($"{recomandare} was linked with {latime}x{lungime} dimension");
                }
                else
                {
                    _docsLogger.LogInformation("No recomandare pat was linked with this dimension");
                }
            }
            dimensionsIdList.Add(isDimensionInDatabase.IdDimensiune);
        }
    }


    private void HandleEmptyDimensions(string productType)
    {
        if (string.Equals(productType, "cuvertura"))
        {
            _docsLogger.LogError("Dimensiuni section cannot be empty!");
            throw new Exception("Dimensiuni section cannot be empty! Rolling back transactions");
        }
        else if (string.Equals(productType, "perdea"))
        {
            _docsLogger.LogWarning($"{productType} was the type of the product! Dimensions section is empty");
        }
        else
        {
            _docsLogger.LogError($"{productType} unknown type of product");
            throw new Exception($"{productType} unknown type of product. Rolling back transactions");
        }
    }
    
    private bool IsRowEmpty(ExcelRow row)
    {
        foreach (var cell in row.AllocatedCells)
        {
            if (!string.IsNullOrWhiteSpace(cell.StringValue))
            {
                return false; 
            }
        }
        return true; 
    }
    
    public async Task ReadProductsExcel(Stream stream)
    {
        SpreadsheetInfo.SetLicense(_freeKey);
        var producatoriRepository = _unitOfWork.Repository<Producatori>();
        var culoriRepository = _unitOfWork.Repository<Culori>();
        var codCuloriRepository = _unitOfWork.Repository<CodCulori>();
        var tipuriProduseRepository = _unitOfWork.Repository<TipuriProduse>();
        var materialeRepository = _unitOfWork.Repository<Materiale>();
        
        ExcelFile workbook = ExcelFile.Load(stream);

        await using var dbContextTransaction = await _unitOfWork.BeginTransactionAsync();
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

                    int idProducator = 0;
                    var row = worksheet.Rows[i];
                    _docsLogger.LogInformation($"Row : {row}");
                    
                    if (IsRowEmpty(row))
                    {
                        _docsLogger.LogInformation($"Skipping empty row {i + 1}");
                        continue;
                    }

                    var tipProdus = row.Cells[15].StringValue.ToLower().Trim();

                    var codProdus = row.Cells[0].StringValue.Trim().ToUpper();


                    var descriereProdus = row.Cells[1].StringValue.Trim();
                    var numeProdus = row.Cells[2].StringValue.Trim();
                    var compozitieProdus = row.Cells[3].StringValue.Trim();
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

                    // greutate processing
                    var greutateProdusString = row.Cells[7].StringValue.Trim();
                    decimal greutateProdus = 0;
                    if (!string.Equals(greutateProdusString, "-") && !string.IsNullOrEmpty(greutateProdusString))
                    {
                        greutateProdus = decimal.Parse(greutateProdusString);
                    }

                    // fata reversibila
                    var fataReversibila = row.Cells[8].StringValue.ToUpper().Trim() == "TRUE";
                  
                    // stoc produs
                    var stocProdusString = row.Cells[9].StringValue.Trim();
                    ushort stocProdus = 0;
                    if (!string.Equals(stocProdusString, "-") && !string.IsNullOrEmpty(stocProdusString))
                    {
                        stocProdus = ushort.Parse(stocProdusString);
                    }

                    var numeProducator = row.Cells[10].StringValue.ToUpper().Trim();

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
                    var dimensiuniFromExcel = row.Cells[11].StringValue.Trim();
                    var recomandarePatFromExcelRow = row.Cells[12].StringValue.Trim();

                    var recomandarePatArrayValues = recomandarePatFromExcelRow.Split(",");
                    var dimensiuniArray = dimensiuniFromExcel.Split(",");

                  

                    if (string.IsNullOrEmpty(recomandarePatFromExcelRow) || recomandarePatFromExcelRow == "-")
                    {
                        if (!string.IsNullOrEmpty(dimensiuniFromExcel) && dimensiuniFromExcel != "-")
                        {
                            await ProcessDimensions(dimensiuniArray, null, dimensionsIdList);
                        }
                        else
                        {
                            HandleEmptyDimensions(tipProdus);
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
                                throw new Exception("Mismatch between dimensions and recomandare pat arrays.");
                            }
                        }
                        else
                        {
                            HandleEmptyDimensions(tipProdus);
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
                            "Prices , dimensions and recomandare pat values are not equal in length");
                    }

                    // PROCESAREA CULORILOR DIN EXCEL
                    // initial -> grey-03,blue-08
                    var culoriString = row.Cells[13].StringValue.ToLower().Trim();

                    if (string.Equals(culoriString, "-") && string.IsNullOrEmpty(culoriString))
                    {
                        _docsLogger.LogError("Culori section cannot be empty ");
                        throw new Exception("Culori section cannot be empty. Rolling back transactions!");
                    }

                    var culoriArray = culoriString.Split(","); // will be parsed !

                    foreach (var culoriPair in culoriArray)
                    {
                        var currentPair = culoriPair.Split("-");
                        var numeCuloare = currentPair[0].Trim();
                        var codCuloare = currentPair[1].Trim();
                        
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

                        var isCuloareInDb = await culoriRepository
                            .FindQueryable(c =>
                                c.NumeCuloare == numeCuloare && c.IdCodCuloare == isCodCuloareInDb.IdCodCuloare)
                            .FirstOrDefaultAsync();

                        if (isCuloareInDb is null)
                        {
                            var newCuloare = new Culori
                            {
                                NumeCuloare = numeCuloare.ToLower(),
                                IdCodCuloare = isCodCuloareInDb.IdCodCuloare
                            };
                            await culoriRepository.AddAsync(newCuloare);
                            await _unitOfWork.CommitAsync();
                            isCuloareInDb = newCuloare;
                            _docsLogger.LogInformation(
                                $"Culoare({isCuloareInDb.NumeCuloare} - cod(FK)-> {isCuloareInDb.IdCodCuloare}) saved succesfully");
                            colorIdList.Add(isCuloareInDb.IdCuloare);
                        }else
                        {
                            colorIdList.Add(isCuloareInDb.IdCuloare);
                            _docsLogger.LogInformation($"Culoare  {numeCuloare} already was in db");
                        }
                        

                    }

                    // processing the category of the products
                    var categoriiProdus = row.Cells[14].StringValue.ToUpper().Trim();

                    if (string.Equals(categoriiProdus, "-") && string.IsNullOrEmpty(categoriiProdus))
                    {
                        _docsLogger.LogError("Category cannot be null.");
                        throw new Exception("Category cannot be null. Rolling back transactions");
                    }

                    var arrayCateogriiProdus = categoriiProdus.Split(","); // will be parsed!
                    foreach (var categorie in arrayCateogriiProdus)
                    {
                        var isCategorieInDatabase = await tipuriProduseRepository
                            .FindQueryable(tp => tp.Categorie == categorie)
                            .FirstOrDefaultAsync();

                        if (isCategorieInDatabase is null)
                        {

                            var newCategorie = new TipuriProduse(tipProdus.ToUpper(), categorie, null);
                            await tipuriProduseRepository.AddAsync(newCategorie);
                            await _unitOfWork.CommitAsync();
                            _docsLogger.LogInformation("Categorie (cuvertura/perdea) added in the database");
                            isCategorieInDatabase = newCategorie;

                        }

                        tipuriProduseIdList.Add(isCategorieInDatabase.IdTipProdus);
                    }


                    if (string.Equals(tipProdus, "perdea"))
                    {
                        var pretLaMetru = row.Cells[16].StringValue.Trim();

                        if (string.Equals(pretLaMetru, "-") && string.IsNullOrEmpty(pretLaMetru))
                        {
                            _docsLogger.LogError("Price for the meter of manopera cannot be null");
                            throw new Exception("Price for the meter cannot be null. Rolling back transactions");
                        }

                        var material = row.Cells[17].StringValue.ToLower();

                        if (string.Equals(material, "-") && string.IsNullOrEmpty(material))
                        {
                            _docsLogger.LogError("Material cannot be null");
                            throw new Exception("Material cannot be null. Rolling back transactions");
                        }

                        // parsing the strings to DB data types

                        var pretLaMetruInt = int.Parse(pretLaMetru);

                        var isMaterialInDb = await materialeRepository
                            .FindQueryable(mat => mat.NumeMaterial == material
                                                  && mat.PretMaterial == pretLaMetruInt)
                            .FirstOrDefaultAsync();

                        if (isMaterialInDb is null)
                        {
                            var newMaterial = new Materiale
                            {
                                NumeMaterial = material,
                                PretMaterial = pretLaMetruInt
                            };

                            await materialeRepository.AddAsync(newMaterial);
                            await _unitOfWork.CommitAsync();
                            isMaterialInDb = newMaterial;
                            _docsLogger.LogInformation(
                                $"Added material:{isMaterialInDb.NumeMaterial} with price {isMaterialInDb.PretMaterial}");
                        }
                    }



                    // images processing
                    var relativePathOfImagesString = row.Cells[18].StringValue;
                    string[] relativePathOfImagesArray = [];

                    if (!string.Equals(relativePathOfImagesString, "-") &&
                        !string.IsNullOrEmpty(relativePathOfImagesString))
                    {
                        relativePathOfImagesArray = relativePathOfImagesString.Split(',');
                    }
                    else
                    {
                        _docsLogger.LogInformation($"No images were chosen for product: {codProdus} \n" +
                                                   $"See line {row.Name}");
                    }

                    var newProdusDto = new ProduseDto
                    {
                        CodProdusDto = codProdus,
                        DescriereDto = descriereProdus,
                        NumeProdusDto = numeProdus,
                        CompozitieDto = compozitieProdus,
                        TvaDto = tvaProdus,
                        IngrijireDto = ingrijireProdus,
                        GreutateDto = greutateProdus,
                        FataReversibilaDto = fataReversibila,
                        StocDto = stocProdus,
                        IsDeletedDto = false,
                        ActivInMagazinDto = false,
                        TipProdusDto = tipProdus
                    };

                    var folderName = row.Cells[19].StringValue.ToLower().Trim();
                    
                    var responseAddProduct = await _productService.AddOrEditProductFromExcel(newProdusDto, dimensionsIdList,
                        relativePathOfImagesArray, tipuriProduseIdList, colorIdList,
                        pretProdusArray, idProducator, tipProdus, folderName);

                    if (responseAddProduct == 1)
                    {
                        _docsLogger.LogInformation($"Product with id: {codProdus} was succesfully added");

                    }else if (responseAddProduct == 2)
                    {
                        _docsLogger.LogInformation($"Product with id: {codProdus} has been updated");
                    }
                    else
                    {
                        _docsLogger.LogError($"Error when processing product: {codProdus}");
                        throw new Exception($"Error when processing product: {codProdus}");
                    }

                }
            }

            await _unitOfWork.CommitTransactionAsync(dbContextTransaction);
        }
        catch (Exception e)
        {
            if (dbContextTransaction == null)
            {
                _docsLogger.LogInformation($"Rolling back transaction!");
                await _unitOfWork.RollBackTransactionAsync(dbContextTransaction!);
            }
            _docsLogger.LogDebug("Error : " + e.Message);
            throw;
        }
    }
}
    