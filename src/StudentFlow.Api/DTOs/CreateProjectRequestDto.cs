using System.ComponentModel.DataAnnotations;

namespace StudentFlow.Api.DTOs;

public class CreateProjectRequestDto : IValidatableObject
{
    [Required(ErrorMessage = "Zadejte název projektu.")]
    [StringLength(100, ErrorMessage = "Název projektu může mít nejvýše 100 znaků.")]
    public string? Name { get; init; }

    [StringLength(1000, ErrorMessage = "Popis může mít nejvýše 1000 znaků.")]
    public string? Description { get; init; }

    [Required(ErrorMessage = "Zadejte plánovaný termín odevzdání.")]
    public DateOnly? Deadline { get; init; }

    [Required(ErrorMessage = "Zadejte název týmu.")]
    [StringLength(100, ErrorMessage = "Název týmu může mít nejvýše 100 znaků.")]
    public string? TeamName { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Deadline < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            yield return new ValidationResult(
                "Termín odevzdání nesmí být v minulosti.", [nameof(Deadline)]);
        }
    }
}
