using Microsoft.AspNetCore.Mvc.ModelBinding;
using panelapp.Models;

public interface IMaterialService
{
    void NormalizeMaterial(Material model);

    bool ValidateMaterialUnit(Material model, ModelStateDictionary modelState);

    Task<bool> MaterialCodeExistsForSupplierAsync(
        int supplierId,
        string materialCode,
        int? excludeMaterialId = null);
}