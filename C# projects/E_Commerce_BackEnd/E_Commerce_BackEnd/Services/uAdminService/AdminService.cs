using System.Diagnostics;
using AutoMapper;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Models.ProductVouchersModels;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret.AWSBucket_CRUD;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.IdentityModel.Tokens;

namespace E_Commerce_BackEnd.Services.uAdminService;

public class AdminService : IAdminService
{
    private readonly ILogger<AdminService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBucketAcces _bucketAcces;
    private IMapper _mapper;

    public AdminService(ILogger<AdminService> logger, IUnitOfWork unitOfWork, IMapper mapper, IBucketAcces bucketAcces)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _bucketAcces = bucketAcces;
    }

    public async Task<int> AdminLogIn(string key)
    {
        var secret = await TokenService.GetSecret("prod/texx.ro/admin");
        
        if (secret == key)
        {
            _logger.LogInformation("key was equal");
            return 1;
        }
        _logger.LogInformation("key was not equal");
        return 0;
    }

    private async Task<IList<ProduseDtoForAdminListing>> TransformProductsListIntoDto(IList<Produse> products)
    {
        var productTypeRepository = _unitOfWork.Repository<TipuriProduse>();
        var dtoList = new List<ProduseDtoForAdminListing>();

        foreach (var product in products)
        {
            var listOfCurrentProductTypes = product.PTipuriPeProduse!;
            var categories = new List<string>();
            var tipProdus = "";
            foreach (var currentTypeOnProduct in listOfCurrentProductTypes)
            {
                var currentType = await productTypeRepository
                    .GetByIdAsync(currentTypeOnProduct.IdTipProdus);
                categories.Add(currentType!.Categorie);
                tipProdus = currentType.TipProdus;
            }

            dtoList.Add(new ProduseDtoForAdminListing
            {
                CodProdusAdminDto = product.CodProdus,
                NumeProdusAdminDto = product.NumeProdus,
                TipProdusDto = tipProdus,
                CategoriiProdusDto = categories
            });
        }

        return dtoList;
    }


    public async Task<IList<ProduseDtoForAdminListing>?> GetProductsForDtoAdminListing()
    {
        var timer = new Stopwatch();
        timer.Start();
        var productsRepository = _unitOfWork.Repository<Produse>();

        var productsQueryable = await productsRepository
            .FindQueryableOfEntitiesAsync(prod => prod.IsDeleted == false,
                nav => nav.PTipuriPeProduse!);

        if (productsQueryable is null || productsQueryable.IsNullOrEmpty())
        {
            _logger.LogInformation("E NULL QEURYABLE");
            return null;
        }

        var productsList = await productsQueryable.ToListAsync();
        
        var dtoListToReturn = await TransformProductsListIntoDto(productsList);
        
        
        
        await Task.CompletedTask;
        _logger.LogInformation($"ELAPSED FOR RETRIEVING LIST OF PRODUCTS:{timer.ElapsedMilliseconds}");
        
        foreach (var product in dtoListToReturn)
        {
            _logger.LogInformation($"product code: {product.CodProdusAdminDto}");
        }
        return dtoListToReturn;
    }

    public async Task<ProduseDtoForAdminModification?> GetProductForAdminPage(string codProdus)
    {
        var productRepository = _unitOfWork.Repository<Produse>();
        var product = await productRepository
            .FindQueryable(p => p.CodProdus == codProdus)
            .Include(p => p.Producator)
            .Include(pd => pd.PProduseCuDimensiuni!)
                .ThenInclude(d => d.PdDimensiune)
            .Include(pt => pt.PTipuriPeProduse!)
                .ThenInclude(t => t.TppTipProdus)
            .Include(pc => pc.PProduseCuCulori!)
                .ThenInclude(c => c.Culoare)
                    .ThenInclude(cc => cc.CodCuloare)
            .Include(pc => pc.PProduseCuCulori!)
                .ThenInclude(i => i.ImagProduseCuCulori)
            .Include(pv => pv.PvProduse!)
                .ThenInclude(v => v.PvVoucher)
            .Include(sa => sa.PAsociereSeturi!)
                .ThenInclude(s => s.Set)
            .AsSplitQuery()
            .FirstOrDefaultAsync();

        if (product is null)
        {
            return null;
        }
        
        var productDto = new ProduseDtoForAdminModification
        {
            CodProdusDto = product.CodProdus,
            DescriereDto = product.Descriere,
            NumeProdusDto = product.NumeProdus,
            CompozitieDto = product.Compozitie,
            TvaDto = product.Tva,
            IngrijireDto = product.Ingrijire,
            GreutateDto = product.Greutate,
            FataReversibilaDto = product.FataReversibila,
            StocDto = product.Stoc,
            ActivInMagazinDto = product.ActivInMagazin,
            NumeProducatorDto = product.Producator == null ? "" : product.Producator.NumeProducator,
            TipulProdusuluiDto = product.TipulProdusului,
            TipuriProduseDto = product.PTipuriPeProduse!.Select(tp => new TipuriProdusDto
            {
                TipProdusDto = tp.TppTipProdus.TipProdus,
                CategorieDto = tp.TppTipProdus.Categorie,
                JustAdded = false
            }).ToList(),
            DimensiuniProduseDto = product.PProduseCuDimensiuni!.Select(pd => new DimensiuniDto
            {
                LungimeDto = pd.PdDimensiune!.Lungime,
                LatimeDto =  pd.PdDimensiune!.Latime,
                RecomandarePat =  pd.PdDimensiune!.RecomandarePat,
                PretDto = pd.Pret,
                PretRedusDto = pd.PretRedus,
                PerdeaEstePerecheDto = pd.PdDimensiune!.PerdeaEstePereche,
                JustAdded = false
            }).ToList(),
            CuloriProdusDto = product.PProduseCuCulori!.Select(pc => new CuloriDto
            {
                NumeCuloareDto = pc.Culoare.NumeCuloare,
                CodCuloareDto = pc.Culoare.CodCuloare.CodCuloare!,
                JustAdded = false,
                ImaginiProdusDto = pc.ImagProduseCuCulori!
                    .Select(imag => new ImagesDto
                    {
                        CaleImagineDto = imag.CaleImagine!,
                        FisierInBucketDto = imag.FisierInBucket,
                        PresignedUrl = GetPresignedUrlFromBucket(imag.CaleImagine!,imag.FisierInBucket).Result,
                        IdProdusCuCuloareDto = pc.IdProdusCuCuloare,
                        JustAdded = false
                    }).ToList()
            }).ToList(),
            SeturiProdusDto = product.PAsociereSeturi!.Select(sa => new SeturiDto
            {
                NumeSetDto = sa.Set.NumeSet,
                DescriereSetDto = sa.Set.DescriereSet,
                JustAdded = false
            }).ToList(),
            VouchereProdusDto = product.PvProduse!.Select(pv => new VouchereDto
            {
                CodVoucherDto = pv.PvVoucher.CodVoucher,
                ReducereDto = pv.PvVoucher.Reducere,
                ExpirareDto = pv.PvVoucher.DataExpirare,
                JustAdded = false
            }).ToList(),
        };
        
        return productDto;
    }

    private async Task<string?> GetPresignedUrlFromBucket(string imagePath, string dirInBucket)
    {
        var url = await _bucketAcces.GenerateUrl(imagePath, dirInBucket);
        return url;
    }
}