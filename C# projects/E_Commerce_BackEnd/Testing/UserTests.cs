using AutoMapper;
using E_Commerce_BackEnd.Models.DTO;
using E_Commerce_BackEnd.Models.OrderRelatedModels;
using E_Commerce_BackEnd.Models.UserRelatedModels;
using E_Commerce_BackEnd.Repositories;
using E_Commerce_BackEnd.Services.emailService;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret;
using E_Commerce_BackEnd.UnitOfWork;
using E_Commerce_BackEnd.Services.uService;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace Testing;
using NUnit.Framework;

public class Tests
{
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private Mock<IMapper> _mockMapper;
    private Mock<IEmailService> _mockEmailService;
    private Mock<ITokenService> _mockTokenService;
    private Mock<IMemoryCache> _mockCache;
    private Mock<ILogger<Conturi>> _mockLoguri;
    private IUserService _userService;

    [SetUp]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockEmailService = new Mock<IEmailService>();
        _mockTokenService = new Mock<ITokenService>();
        _mockCache = new Mock<IMemoryCache>();
        _mockCache = new Mock<IMemoryCache>();
        
        _userService = new UserService(_mockUnitOfWork.Object, _mockMapper.Object, _mockEmailService.Object, 
            _mockTokenService.Object , _mockCache.Object , _mockLoguri.Object);
    }

    [Test]
    public async Task GetProfileDataAsync_ShouldReturnDto()
    {
       // Arrange 

       int userId = 1;

       
       var mockConturi = new Conturi
       {
           IdCont = 0,
           Nume = null,
           Prenume = null,
           Gen = null,
           NrTelefon = null,
           Username = null,
           Email = null,
           Parola = null,
           DataCreare = null,
           CodActivare = null,
           Verificat = false,
           Rol = null,
           OraLinkConfirmare = default,
           AdreseConturi = null
       };

       var expectedDto = new ConturiDto
       {
           Nume = null,
           Prenume = null,
           Gen = null,
           NrTelefon = null,
           Email = "aade211@yahoo.com"
       };
        
       var mockRepository = new Mock<IRepository<Conturi>>();
       _mockUnitOfWork.Setup(u => u.Repository<Conturi>()).Returns(mockRepository.Object);
       mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(mockConturi);
       _mockMapper.Setup(m => m.Map<ConturiDto>(mockConturi)).Returns(expectedDto);
       
       // Act
       var result = await _userService.GetProfileDataAsync(userId);
       
       // Assert
       Assert.That(result, Is.EqualTo(expectedDto));
    }
    
}