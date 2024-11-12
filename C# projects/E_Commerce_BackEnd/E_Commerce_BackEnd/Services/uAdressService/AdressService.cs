using AutoMapper;
using E_Commerce_BackEnd.Models.DTO;
using E_Commerce_BackEnd.Models.Enums;
using E_Commerce_BackEnd.Models.OrderRelatedModels;
using E_Commerce_BackEnd.Models.UserRelatedModels;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace E_Commerce_BackEnd.Services.uAdressService;

public class AdressService : IAdressService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<Adrese> _logger;
    private readonly IMemoryCache _cache;
    private readonly IMapper _mapper;

    public AdressService(IUnitOfWork unitOfWork, ILogger<Adrese> logger, IMemoryCache cache, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cache = cache;
        _mapper = mapper;
    }

    public async Task<IList<AdreseDto>?> GetAllUsersAdresses(int userId)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        
        var cacheKey = $"Adrese_{userId}";

        if (_cache.TryGetValue(cacheKey, out IList<AdreseDto>? adreseInCache))
        {
            _logger.LogInformation("RETURNED FROM CACHE");
            return adreseInCache;
        }
        
        var adressRepository =  _unitOfWork.Repository<Adrese>();
        
        var allUserAdressessQueryable = await adressRepository
            .FindQueryableOfEntitiesAsync(a => a.IdCont == userId && a.IsDeleted == false ,
            a => a.Locatie,
            a => a.DetaliuFactura!);
        

        if (allUserAdressessQueryable == null)
        {
            return null;
        }

        var addressList = await allUserAdressessQueryable.ToListAsync();
        
        
        var userAddressesDto = _mapper.Map<IList<AdreseDto>>(addressList);
        
        _cache.Set(cacheKey, userAddressesDto, TimeSpan.FromMinutes(10)); 
        
        watch.Stop();
        _logger.LogInformation($"Time elapsed in AddressService for fetching the adresses: {watch.ElapsedMilliseconds}");

        return userAddressesDto;

    }
    
    // helper
    private async void AddOrDeleteFromCacheStorage(AdreseDto newAddressDto , int userId , bool option)
    {
       
        var cacheKey = $"Adrese_{userId}";
       

        if (_cache.TryGetValue(cacheKey, out IList<AdreseDto>? cachedAddresses))
        {
            // true for insertion
            // false for deletion
           
            
            if (option)
            {
                cachedAddresses!.Add(newAddressDto);
                
            }
            else
            {
                
                var addressToRemoveFromCache = cachedAddresses!
                    .FirstOrDefault(a => a.AliasDto == newAddressDto.AliasDto);

                if (addressToRemoveFromCache == null)
                {
                    _logger.LogWarning("Address already removed or not present in the cache");
                    return;
                }
                cachedAddresses!.Remove(addressToRemoveFromCache);
                
                if (cachedAddresses.Count == 0)
                {
                    _logger.LogInformation("EMPTY CACHE");
                    _cache.Remove(cacheKey);
                }
                _logger.LogInformation("Address removed succesfully from cache storage");
               
            }

            _cache.Set(cacheKey, cachedAddresses, TimeSpan.FromMinutes(10));

        }
        else
        {
            // If not in cache, fetch from DB and set cache
            var allUserAddressesQueryable = await _unitOfWork.Repository<Adrese>().FindQueryableOfEntitiesAsync(
                a => a.IdCont == userId && a.IsDeleted == false,
                a => a.Locatie,
                a => a.DetaliuFactura!);

            if (allUserAddressesQueryable == null)
            {
                return;
            }

            var allUserAddresses = _mapper.Map<AdreseDto>(await allUserAddressesQueryable.ToListAsync()) ;
            
            _cache.Set(cacheKey, allUserAddresses, TimeSpan.FromMinutes(10));
        }

    }

    public async Task<int> SaveAddress(AdreseDto adressDto, int userId)
    {
        IDbContextTransaction transaction = null!;
        try
        {
            transaction = await _unitOfWork.BeginTransactionAsync();
        
        
            var adreseRepository = _unitOfWork.Repository<Adrese>();
            var conturiRepository = _unitOfWork.Repository<Conturi>();
            var locationsRepository = _unitOfWork.Repository<Locatii>();
            var detaliiFacturaRepository = _unitOfWork.Repository<DetaliiFactura>();
            
            
            var currentUserLoggedIn = await conturiRepository.GetByIdAsync(userId);
            var currentLocatie = await locationsRepository.FindQueryable(a => a.Oras == adressDto.OrasDto &&
                a.Judet == adressDto.JudetDto && a.CodPostal == adressDto.CodPostalDto).FirstOrDefaultAsync();


            if (currentLocatie == null)
            {
                var newLocation = new Locatii
                (
                    adressDto.OrasDto,
                    adressDto.JudetDto,
                    adressDto.CodPostalDto,
                    new HashSet<Adrese>()
                );

                await locationsRepository.AddAsync(newLocation);
                await _unitOfWork.CommitAsync();

                currentLocatie = newLocation;
                
            }

            Adrese addressToBeSaved;

            if (adressDto.TipAdresaDto == TipAdrese.Livrare)
            {
                addressToBeSaved = new Adrese
                {
                    Alias = adressDto.AliasDto,
                    TipAdresa = adressDto.TipAdresaDto,
                    Bloc =  adressDto.BlocDto,
                    NrBloc = adressDto.NrBlocDto,
                    Strada =  adressDto.StradaDto,
                    NrStrada = adressDto.NrStradaDto,
                    IsDeleted = false,
                    IdLocatie = currentLocatie.IdLocatie,
                    IdCont = currentUserLoggedIn!.IdCont,
                    IdDetaliuFactura = null
                };
            }
            else
            {
                var billingDetalils = new DetaliiFactura
                {
                    Cif = adressDto.CifDto,
                    NumeFirma = adressDto.NumeFirmaDto,
                };
            

                await detaliiFacturaRepository.AddAsync(billingDetalils);
                await _unitOfWork.CommitAsync();

                addressToBeSaved = new Adrese
                {
                    Alias = adressDto.AliasDto,
                    TipAdresa = adressDto.TipAdresaDto,
                    Bloc = adressDto.BlocDto,
                    NrBloc = adressDto.NrBlocDto,
                    Strada = adressDto.StradaDto,
                    NrStrada = adressDto.NrStradaDto,
                    IsDeleted = false,
                    IdLocatie = currentLocatie.IdLocatie,
                    IdCont = currentUserLoggedIn!.IdCont,
                    IdDetaliuFactura = billingDetalils.IdDetaliu
                };
            }
            
            await adreseRepository.AddAsync(addressToBeSaved);
            await _unitOfWork.CommitAsync();
            

                
            
            await _unitOfWork.CommitTransactionAsync(transaction);
            
            AddOrDeleteFromCacheStorage(_mapper.Map<AdreseDto>(addressToBeSaved) , userId , true);
            
            _logger.LogInformation("Adress succesfully saved!");
            
            
            return 1;

        }
        catch (Exception e)
        {
            if (transaction == null!)
            {
                _logger.LogCritical("Adding adress failed , rolling back transaction");
                await _unitOfWork.RollBackTransactionAsync(transaction!);
            }
            Console.WriteLine(e);
            return -1;
        }
       
    }
    // de modificat in asa fel daca clientul nu are nicio comanda pe adresa respectica 
    // sa se stearga de tot
    // de asemenea , de rezolvat de ce cand mai e doar o adresa , nu se sterge si se arata formularul cu adresa care trebuia stearsa
    public async Task<int> DeleteAddress(int userId, string alias)
    {
        IDbContextTransaction? transaction = null;

        try
        {

            transaction = await _unitOfWork.BeginTransactionAsync();
            
            var addressRepository = _unitOfWork.Repository<Adrese>();
            

            var addressToDelete = await addressRepository
                .FindQueryable(address => address.IdCont == userId && address.Alias == alias)
                    .Include(prop => prop.AdreseFacturarePeComanda)
                .Include(prop => prop.AdreseLivrarePeComanda)
                    .FirstOrDefaultAsync();

            if (addressToDelete == null)
            {
                _logger.LogWarning("Address not found for deletion.");
                return -1; 
            }

            // Safely check if there are any orders associated with the address
            var addressHasOrders = addressToDelete.AdreseFacturarePeComanda.IsNullOrEmpty() &&
                                    addressToDelete.AdreseLivrarePeComanda.IsNullOrEmpty();
           
            if (addressHasOrders)
            {
                _logger.LogInformation("Addres has no orders, can safe delete");
                await addressRepository.DeleteAsync(addressToDelete);
            }
            else
            {
                addressToDelete.IsDeleted = true;
                await addressRepository.UpdateAsync(addressToDelete);
            }

            
            AddOrDeleteFromCacheStorage(_mapper.Map<AdreseDto>(addressToDelete), userId, false);
            
            _logger.LogInformation("Address deleted successfully along with all its related entities");
            await _unitOfWork.CommitTransactionAsync(transaction);
            return 1;
        }
        catch (Exception e)
        {
            if (transaction != null)
            {
                _logger.LogCritical("Deleting address failed, rolling back transaction");
                await _unitOfWork.RollBackTransactionAsync(transaction);
            }

            Console.WriteLine(e);
            return -1;
        }
    }

}