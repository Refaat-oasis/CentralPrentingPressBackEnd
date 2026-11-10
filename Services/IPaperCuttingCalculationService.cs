using CentralPress.API.DTOs;
using CentralPress.API.ViewModels;

namespace CentralPress.API.Services;

public interface IPaperCuttingCalculationService
{
    PaperCuttingCalculationResultViewModel CalculateCuttingYield(PaperCuttingCalculationRequestDto request);
}
