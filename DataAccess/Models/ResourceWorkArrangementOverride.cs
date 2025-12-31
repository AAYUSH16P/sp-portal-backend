using System;
using System.Collections.Generic;

namespace FinancialManagementDataAccess.Models;

public partial class ResourceWorkArrangementOverride
{
    public Guid Id { get; set; }

    public Guid? ResourceId { get; set; }

    public decimal? DayRate { get; set; }

    public decimal? Tolerance { get; set; }

    public decimal? TaDa { get; set; }

    public decimal? NearDelivery { get; set; }

    public decimal? McbCrm { get; set; }

    public decimal? Ws { get; set; }

    public decimal? DabDelphi { get; set; }

    public decimal? DabApex { get; set; }

    public decimal? DabBigData { get; set; }

    public decimal? Saiven { get; set; }

    public decimal? Bau { get; set; }

    public decimal? SpectrumProfit { get; set; }

    public decimal? BasicRate { get; set; }

    public virtual Resource? Resource { get; set; }
}
