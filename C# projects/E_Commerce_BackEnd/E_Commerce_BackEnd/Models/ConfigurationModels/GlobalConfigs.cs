namespace E_Commerce_BackEnd.Models.ConfigurationModels;

public class GlobalConfigs
{
    public int IdConfiguratie { get; init; }
    public string NumeAtributGlobal { get; init; } = null!;
    public string ValoareAtributGlobal { get; set; } = null!;
   
}