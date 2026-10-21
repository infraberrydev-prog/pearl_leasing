using FluentValidation;

namespace pearl_leasing.DTO
{
    public record DownloadReportsRequest(
        List<int> ReportIds
    );

    public class DownloadReportsRequestValidator : AbstractValidator<DownloadReportsRequest>
    {
        public DownloadReportsRequestValidator()
        {
            RuleFor(x => x.ReportIds)
                .NotNull().WithMessage("At least one report ID is required.")
                .NotEmpty().WithMessage("At least one report ID is required.")
                .Must(ids => ids.Distinct().Count() <= 100).WithMessage("A maximum of 100 reports can be downloaded at once.");

            RuleForEach(x => x.ReportIds)
                .GreaterThan(0).WithMessage("Report IDs must be greater than zero.");
        }
    }
}
