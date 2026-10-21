using FluentValidation;

namespace pearl_leasing.DTO
{
    public record UpdateChildRequest
    (
        int Id,
        int BuildingId,
        int StallUnitId,
        string Building,
        string VendorNameOrCategory,
        string StallUnit,
        string NameOfLessee,
        string? MobileNumber,
        decimal MonthlyRental,
        string PaymentStatus,
        string PaymentMethod,
        string ReferenceNumber,
        decimal Electricity,
        decimal Water,
        decimal OtherUtilities,
        decimal? CusaFee,
        decimal? PylonRental,
        decimal? PenaltyTenPercent
    );

    public class UpdateChildRequestValidator : AbstractValidator<UpdateChildRequest>
    {
        public UpdateChildRequestValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.BuildingId).GreaterThan(0);
            RuleFor(x => x.StallUnitId).GreaterThan(0);
            RuleFor(x => x.Building).NotEmpty();
            RuleFor(x => x.VendorNameOrCategory).NotEmpty();
            RuleFor(x => x.StallUnit).NotEmpty();
            RuleFor(x => x.NameOfLessee).NotEmpty();
            RuleFor(x => x.MonthlyRental).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Electricity).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Water).GreaterThanOrEqualTo(0);
            RuleFor(x => x.OtherUtilities).GreaterThanOrEqualTo(0);
            RuleFor(x => x.CusaFee).GreaterThanOrEqualTo(0).When(x => x.CusaFee.HasValue);
            RuleFor(x => x.PylonRental).GreaterThanOrEqualTo(0).When(x => x.PylonRental.HasValue);
            RuleFor(x => x.PenaltyTenPercent).GreaterThanOrEqualTo(0).When(x => x.PenaltyTenPercent.HasValue);
        }
    }

    public class AddSingleRowRequestValidator : AbstractValidator<AddSingleRowRequest>
    {
        public AddSingleRowRequestValidator()
        {
            RuleFor(x => x.BuildingId).GreaterThan(0);
            RuleFor(x => x.StallUnitId).GreaterThan(0);
            RuleFor(x => x.VendorNameOrCategory).NotEmpty();
            RuleFor(x => x.NameOfLessee).NotEmpty();
            RuleFor(x => x.MonthlyRental).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Electricity).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Water).GreaterThanOrEqualTo(0);
            RuleFor(x => x.OtherUtilities).GreaterThanOrEqualTo(0);
            RuleFor(x => x.CusaFee).GreaterThanOrEqualTo(0).When(x => x.CusaFee.HasValue);
            RuleFor(x => x.PylonRental).GreaterThanOrEqualTo(0).When(x => x.PylonRental.HasValue);
            RuleFor(x => x.PenaltyTenPercent).GreaterThanOrEqualTo(0).When(x => x.PenaltyTenPercent.HasValue);
        }
    }

    // DTO para sa pagdagdag ng bagong row sa umiiral na Report
    public record AddReportRowRequest(
        int BuildingId,
        int StallUnitId,
        string Building,
        string VendorNameOrCategory,
        string StallUnit,
        string NameOfLessee,
        string? MobileNumber,
        string? TinNo,
        decimal MonthlyRental,
        string PaymentStatus,
        string PaymentMethod,
        string ReferenceNumber,
        decimal Electricity,
        decimal Water,
        decimal OtherUtilities,
        decimal? CusaFee,
        decimal? PylonRental,
        decimal? PenaltyTenPercent
    );

    // Legacy DTO alias — align to AddReportRowRequest structure used by the controller
    public record AddSingleRowRequest(
        int BuildingId,
        int StallUnitId,
        string Building,
        string VendorNameOrCategory,
        string StallUnit,
        string NameOfLessee,
        string? MobileNumber,
        string? TinNo,
        decimal MonthlyRental,
        string PaymentStatus,
        string PaymentMethod,
        string ReferenceNumber,
        decimal Electricity,
        decimal Water,
        decimal OtherUtilities,
        decimal? CusaFee,
        decimal? PylonRental,
        decimal? PenaltyTenPercent
    );

    public class AddSingleRowResponse
    {
        public string message { get; set; } = string.Empty;
        public int newRowId { get; set; }
    }
}