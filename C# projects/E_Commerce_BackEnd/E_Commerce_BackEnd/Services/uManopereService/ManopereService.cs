using AutoMapper;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ManopereDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ManopereDto.ManoperaModification;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage.OptionsForCurtain;
using E_Commerce_BackEnd.Models.Enums;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret.AWSBucket_CRUD;
using E_Commerce_BackEnd.Services.Helpers.UserHelpers;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Sqids;

namespace E_Commerce_BackEnd.Services.uManopereService;

public class ManopereService : IManopereService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ManopereService> _logger;
    private readonly SqidsEncoder<int> _sqidsEncoder = new ();
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly IBucketAcces _bucketAcces;

    public ManopereService(IUnitOfWork unitOfWork, ILogger<ManopereService> logger, IMapper mapper, IMemoryCache cache, IBucketAcces bucketAcces)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
        _cache = cache;
        _bucketAcces = bucketAcces;
    }

    public async Task<IList<ManopereListingDto>> GetManopere()
    {
        var manopereRepository = _unitOfWork.Repository<Manopere>();
        var manopereToDto = await manopereRepository  
            .GetSimpleQueryable()
            .Where(m => m.TipManopera == TipManopere.Standard)
            .Select(m => new ManopereListingDto
            {
                EncodedIdManoperaDto = _sqidsEncoder.Encode(m.IdManopera),
                NumeManoperaDto = m.NumeManopera!,
                TipLinieDto = m.TipLinieLaManopera.NumeTipLinie,
                TipInelDto = m.InelPrindereLaManopera!.CuloareInel,
                TipCusaturaDto = m.TipGalerieLaManopera.NumeTipGalerie
            }).ToListAsync();

        return manopereToDto;
    }

    public async Task<ManoperaPageModification?> GetManoperaForModification(int manoperaId , TipManopere tipManopera)
    {
        var manoperaRepository = _unitOfWork.Repository<Manopere>();

        var manopereNames = manoperaRepository
            .FindQueryable(m => m.TipManopera == TipManopere.Standard)
            .Select(m => m.NumeManopera)
            .ToList();

        var availableOptions = await GetAvailableOptions();
        
        var getManoperaForModification = await manoperaRepository
            .GetSimpleQueryable()
            .Where(m => m.IdManopera == manoperaId && m.TipManopera == TipManopere.Standard)
            .Select(m => new ManoperaPageModification
            {
                NumeManopera = m.NumeManopera!,
                TipInel = m.InelPrindereLaManopera != null ? new TipIneleDto
                {
                    NumeTipInel = m.InelPrindereLaManopera!.CuloareInel,
                    CaleRelativa = null,
                    PresignedUrl = "empty"
                } : null,
                TipGalerie = new TipRejansaDto
                {
                    NumeTipRejansa = m.TipGalerieLaManopera.NumeTipGalerie,
                    PretTipRejansa = m.TipGalerieLaManopera.PretTipGalerie,
                    IncretireRejansa = m.TipGalerieLaManopera.IncretireRejansa,
                    CaleRelativa = null,
                    PresignedUrl = "empty",
                    SePrindeCuInele = m.TipGalerieLaManopera.SePrindeCuInele
                },
                TipLinie = new TipLinieDto
                {
                    NumeTipCusaturaColt = m.TipLinieLaManopera.NumeTipLinie,
                    PretTipCusaturaColt = m.TipLinieLaManopera.PretPeTipLinie,
                    CaleRelativa = null,
                    PresignedUrl = "empty"
                },
                MetruTotalFolosit = m.MaterialFolosit,
                InaltimeMaxima = m.InaltimeMaxima,
                NumeDeManopere = manopereNames,
                OptiuniDisponibile = availableOptions
                
            }).FirstOrDefaultAsync();

        if (getManoperaForModification !=  null)
        {
            if (getManoperaForModification.TipInel != null)
            {
                if (getManoperaForModification.TipInel.CaleRelativa != null)
                {
                    getManoperaForModification.TipInel.PresignedUrl = await
                        _bucketAcces.GenerateUrl(getManoperaForModification.TipInel.CaleRelativa, "inele_prindere");
                }

            }

            if (getManoperaForModification.TipGalerie.CaleRelativa != null)
            {
                getManoperaForModification.TipGalerie.PresignedUrl = await
                    _bucketAcces.GenerateUrl(getManoperaForModification.TipGalerie.CaleRelativa, "tipuri_galerie");
            }
            
            if (getManoperaForModification.TipLinie.CaleRelativa != null)
            {
                getManoperaForModification.TipGalerie.PresignedUrl = await
                    _bucketAcces.GenerateUrl(getManoperaForModification.TipLinie.CaleRelativa, "tipuri_linie");
            }
        }
        else
        {
            return null;
        }
        
        return getManoperaForModification;

    }

    public async Task<ManoperaCurtainsOptions> GetAvailableOptions()
    {
        const string currency = "RON";
        var manoperaRepository = _unitOfWork.Repository<Manopere>();
        var manopereNames = manoperaRepository
            .FindQueryable(m => m.TipManopera == TipManopere.Standard)
            .Select(m => m.NumeManopera)
            .ToList();

        var ringTypesDto = await _cache.GetOrCreateAsync($"ringTypes", async entry =>
            {
                var ringTypesRepository = _unitOfWork.Repository<InelePrindere>();
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);

                // If cache does not exist, run the retrieval and mapping logic
                var rings = await ringTypesRepository.GetAllAsync();
                var mappedRings = rings.IsNullOrEmpty() ? new List<TipIneleDto>() : _mapper.Map<IList<TipIneleDto>>(rings);

                // Generate presigned URLs for each ring type
                var ringTasks = mappedRings.Select(async ringType =>
                {
                    ringType.PresignedUrl = ringType.CaleRelativa != null
                        ? await _bucketAcces.GenerateUrl(ringType.CaleRelativa, "inele_prindere")
                        : null;
                }).ToList();

                await Task.WhenAll(ringTasks);

                return mappedRings;
            });

        var rejanseTypesDto = await _cache.GetOrCreateAsync($"rejanse_{currency}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
                var rejansaRepository = _unitOfWork.Repository<TipuriGalerie>();
                var rejanse = await rejansaRepository.GetAllAsync();
                var mappedRejanse = rejanse.IsNullOrEmpty() ? new List<TipRejansaDto>() : _mapper.Map<IList<TipRejansaDto>>(rejanse);

                var rejansaTasks = mappedRejanse.Select(async rejansa =>
                {
                    rejansa.PresignedUrl = rejansa.CaleRelativa != null
                        ? await _bucketAcces.GenerateUrl(rejansa.CaleRelativa, "tipuri_galerie")
                        : null;
                    
                }).ToList();

                await Task.WhenAll(rejansaTasks);

                return mappedRejanse;
            });

        var liningTypesDto = await _cache.GetOrCreateAsync($"liningTypes_{currency}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
                var liningTypesRepository = _unitOfWork.Repository<TipuriLinie>();
                var linings = await liningTypesRepository.GetAllAsync();
                var mappedLinings = linings.IsNullOrEmpty() ? new List<TipLinieDto>() : _mapper.Map<IList<TipLinieDto>>(linings);

                var liningTasks = mappedLinings.Select(async lineType =>
                {
                    lineType.PresignedUrl = lineType.CaleRelativa != null
                        ? await _bucketAcces.GenerateUrl(lineType.CaleRelativa, "tipuri_linie")
                        : null;
                }).ToList();

                await Task.WhenAll(liningTasks);

                return mappedLinings;
            });

        return new ManoperaCurtainsOptions
        {
            RejanseDisponibile = rejanseTypesDto!,
            IneleDisponibile = ringTypesDto!,
            CusaturiLiniiDisponibile = liningTypesDto!,
            NumeManopereFolosite = manopereNames
        };
    }

    public async Task<int> ModifyOrAddManopera(ManoperaPageModification manopera,  bool isUpdating ,  int? idManopera)
    {
        IDbContextTransaction? updateOrAddTransaction = null;
        try
        {
            updateOrAddTransaction = await _unitOfWork.BeginTransactionAsync();
            var manoperaRepository = _unitOfWork.Repository<Manopere>();
            var galeryTypeInDb = await _unitOfWork.Repository<TipuriGalerie>()
                .FindQueryable(tipGalerie => tipGalerie.NumeTipGalerie == manopera.TipGalerie.NumeTipRejansa)
                .FirstAsync();
                
            var liningTypeInDb = await _unitOfWork.Repository<TipuriLinie>()
                .FindQueryable(tipLinie => tipLinie.NumeTipLinie == manopera.TipLinie.NumeTipCusaturaColt)
                .FirstAsync();

            InelePrindere? ringTypeInDb = null;
                
            if (manopera.TipInel != null)
            {
                ringTypeInDb = await _unitOfWork.Repository<InelePrindere>()
                    .FindQueryable(ring => ring.CuloareInel == manopera.TipInel!.NumeTipInel)
                    .FirstAsync();
            }
            if (isUpdating && idManopera != null)
            {
                var manoperaToBeUpdated = await manoperaRepository
                    .FindQueryable(m => m.IdManopera == idManopera
                                        && m.TipManopera == TipManopere.Standard)
                    .FirstAsync();

                _mapper.Map(manopera, manoperaToBeUpdated);
                
                manoperaToBeUpdated.IdTipGalerie = galeryTypeInDb.IdTipGalerie;
                manoperaToBeUpdated.IdTipLinie = liningTypeInDb.IdTipLinie;
                manoperaToBeUpdated.IdInelPrindere = ringTypeInDb?.IdInel;

                
                await manoperaRepository.UpdateAsync(manoperaToBeUpdated);
                await _unitOfWork.CommitTransactionAsync(updateOrAddTransaction);

                return 1;

            }
            else if(!isUpdating && idManopera == null)
            {
                var newManoperaToBeAdded = new Manopere
                {
                    NumeManopera = manopera.NumeManopera,
                    IdInelPrindere = ringTypeInDb?.IdInel,
                    IdTipLinie = liningTypeInDb.IdTipLinie,
                    IdTipGalerie = galeryTypeInDb.IdTipGalerie,
                    MaterialFolosit = manopera.MetruTotalFolosit,
                    InaltimeMaxima = manopera.InaltimeMaxima,
                    TipManopera = TipManopere.Standard
                };

                await manoperaRepository.AddAsync(newManoperaToBeAdded);
                await _unitOfWork.CommitTransactionAsync(updateOrAddTransaction);
                
                
                return 2;
            }
        }
        catch (Exception e)
        {
            if (updateOrAddTransaction != null)
            {
                _logger.LogWarning("Rolling back transaction. Exception occured. Check information below for the reason");
                await _unitOfWork.RollBackTransactionAsync(updateOrAddTransaction);
            }
            
            if (e is not (ArgumentNullException or InvalidOperationException))
            {
                _logger.LogError("Error thrown:  {1}" , e.GetType());
                _logger.LogInformation("Stacktrace : {1}" , e.StackTrace);
                _logger.LogInformation("Message thrown : {1}" , e.Message);
                return -2;
            }
            
            _logger.LogError("Either a galery type , or a ring type , or a lining type does not exist");
            _logger.LogError("Error thrown:  {1}" , e.GetType());
            _logger.LogInformation("Stacktrace : {1}" , e.StackTrace);
            _logger.LogInformation("Message thrown : {1}" , e.Message);
            return -1;

        }

        return 0;
    }
}