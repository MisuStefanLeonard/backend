namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos;

public class ExcelProcessingResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public static ExcelProcessingResult SuccessResult(string message = "Operation successful")
    {
        return new ExcelProcessingResult { Success = true, Message = message };
    }

    public static ExcelProcessingResult ErrorResult(string errorMessage)
    {
        return new ExcelProcessingResult { Success = false, Message = errorMessage };
    }
}
