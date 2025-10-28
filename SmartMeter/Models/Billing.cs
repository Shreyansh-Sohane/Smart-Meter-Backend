using System;
using System.Collections.Generic;

namespace SmartMeter.Models;

public partial class Billing
{
    public int Billid { get; set; }

    public long Consumerid { get; set; }

    public string Meterid { get; set; } = null!;

    public DateOnly Billingperiodstart { get; set; }

    public DateOnly Billingperiodend { get; set; }

    public decimal Totalunitsconsumed { get; set; }

    public decimal Baseamount { get; set; }

    public decimal Taxamount { get; set; }

    public decimal? Totalamount { get; set; }

    public DateTime Generatedat { get; set; }

    public DateOnly Duedate { get; set; }

    public DateTime? Paiddate { get; set; }

    public string Paymentstatus { get; set; } = null!;

    public DateTime? Disconnectiondate { get; set; }

    public virtual ICollection<Arrear> Arrears { get; set; } = new List<Arrear>();

    public virtual Consumer Consumer { get; set; } = null!;

    public virtual Meter Meter { get; set; } = null!;
}
