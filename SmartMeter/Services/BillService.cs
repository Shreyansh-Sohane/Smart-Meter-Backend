//using SmartMeter.Data;
//using SmartMeter.Models;
//using SmartMeter.Models.DTOs;
//using Microsoft.EntityFrameworkCore;

//namespace SmartMeter.Services
//{
//    public class BillService : IBillService
//    {
//        private readonly SmartMeterDbContext _context;
//        private readonly ILogger<BillService> _logger;

//        public BillService(SmartMeterDbContext context, ILogger<BillService> logger)
//        {
//            _context = context;
//            _logger = logger;
//        }

//        public async Task<BillResponseDto> GenerateBillAsync(GenerateBillDto request)
//        {
//            _logger.LogInformation("Generating bill for consumer {ConsumerId} and meter {MeterSerialNo}",
//                request.ConsumerId, request.MeterSerialNo);

//            // 1. Validate consumer exists
//            var consumer = await _context.Consumers
//                .Include(c => c.AidNavigation) // Include Address
//                .FirstOrDefaultAsync(c => c.Consumerid == request.ConsumerId);

//            if (consumer == null)
//                throw new ArgumentException($"Consumer with ID {request.ConsumerId} not found");

//            // 2. Validate meter exists and belongs to consumer
//            var meter = await _context.Meters
//                .FirstOrDefaultAsync(m => m.Meterserialno == request.MeterSerialNo && m.Consumerid == request.ConsumerId);

//            if (meter == null)
//                throw new ArgumentException($"Meter with serial number {request.MeterSerialNo} not found for consumer {request.ConsumerId}");

//            // 3. Get previous reading
//            var previousReading = await GetPreviousReading(request.MeterSerialNo, request.BillingPeriodStart);

//            // 4. Calculate units consumed
//            var unitsConsumed = request.CurrentReading - previousReading;
//            if (unitsConsumed < 0)
//                throw new ArgumentException("Current reading cannot be less than previous reading");

//            // 5. Create billing record
//            var billing = new Billing
//            {
//                Consumerid = request.ConsumerId,
//                Meterid = request.MeterSerialNo,
//                Billingperiodstart = request.BillingPeriodStart,
//                Billingperiodend = request.BillingPeriodEnd,
//                Totalunitsconsumed = unitsConsumed,
//                Baseamount = 0, // We'll calculate this in next part
//                Taxamount = 0,  // We'll calculate this in next part
//                Totalamount = 0, // We'll calculate this in next part
//                Generatedat = DateTime.UtcNow,
//                Duedate = request.BillingPeriodEnd.AddDays(15), // Due date 15 days after billing period
//                Paymentstatus = "Pending"
//            };

//            _context.Billings.Add(billing);
//            await _context.SaveChangesAsync();

//            _logger.LogInformation("Basic bill generated successfully with ID: {BillId}", billing.Billid);

//            // 6. Return response
//            return await MapToBillResponseDto(billing);
//        }

//        public async Task<List<BillResponseDto>> GetConsumerBillsAsync(long consumerId)
//        {
//            var bills = await _context.Billings
//                .Include(b => b.Consumer)
//                    .ThenInclude(c => c.AidNavigation) // Include Address
//                .Include(b => b.Meter)
//                .Where(b => b.Consumerid == consumerId)
//                .OrderByDescending(b => b.Generatedat)
//                .Select(b => MapToBillResponseDto(b))
//                .ToListAsync();

//            return bills;
//        }

//        public async Task<BillResponseDto?> GetBillByIdAsync(int billId)
//        {
//            var billing = await _context.Billings
//                .Include(b => b.Consumer)
//                    .ThenInclude(c => c.AidNavigation) // Include Address
//                .Include(b => b.Meter)
//                .FirstOrDefaultAsync(b => b.Billid == billId);

//            if (billing == null) return null;

//            return await MapToBillResponseDto(billing);
//        }

//        private async Task<decimal> GetPreviousReading(string meterSerialNo, DateOnly billingPeriodStart)
//        {
//            // Get the last reading before the billing period start
//            var previousReadingRecord = await _context.Meterreadings
//                .Where(mr => mr.Meterid == meterSerialNo &&
//                           mr.Meterreadingdate < billingPeriodStart.ToDateTime(TimeOnly.MinValue))
//                .OrderByDescending(mr => mr.Meterreadingdate)
//                .FirstOrDefaultAsync();

//            return previousReadingRecord?.Energyconsumed ?? 0;
//        }

//        private async Task<BillResponseDto> MapToBillResponseDto(Billing billing)
//        {
//            // Build address from Address table
//            string address = "Address not available";
//            if (billing.Consumer.AidNavigation != null)
//            {
//                address = $"{billing.Consumer.AidNavigation.Houseno}, {billing.Consumer.AidNavigation.Lanelocality}, {billing.Consumer.AidNavigation.City}, {billing.Consumer.AidNavigation.State} - {billing.Consumer.AidNavigation.Pincode}";
//            }

//            // Fix for DateOnly to DateTime conversion
//            DateTime? paidDateTime = null;
//            if (billing.Paiddate.HasValue)
//            {
//                //paidDateTime = billing.Paiddate.Value.ToDateTime(TimeOnly.MinValue);
//                paidDateTime = billing.Paiddate;
//            }

//            return new BillResponseDto
//            {
//                BillId = billing.Billid,
//                ConsumerId = billing.Consumerid,
//                MeterSerialNo = billing.Meterid,
//                ConsumerName = billing.Consumer.Name, 
//                Address = address,
//                BillingPeriodStart = billing.Billingperiodstart,
//                BillingPeriodEnd = billing.Billingperiodend,
//                TotalUnitsConsumed = billing.Totalunitsconsumed,
//                BaseAmount = billing.Baseamount,
//                TaxAmount = billing.Taxamount,
//                TotalAmount = billing.Totalamount ?? 0,
//                GeneratedAt = billing.Generatedat,
//                DueDate = billing.Duedate,
//                PaymentStatus = billing.Paymentstatus,
//                PaidDate = paidDateTime
//            };
//        }
//    }
//}





using SmartMeter.Data;
using SmartMeter.Models;
using SmartMeter.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace SmartMeter.Services
{
    public class BillService : IBillService
    {
        private readonly SmartMeterDbContext _context;
        private readonly ILogger<BillService> _logger;

        public BillService(SmartMeterDbContext context, ILogger<BillService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<BillResponseDto> GenerateBillAsync(GenerateBillDto request)
        {
            _logger.LogInformation("Generating bill for consumer {ConsumerId} and meter {MeterSerialNo}",
                request.ConsumerId, request.MeterSerialNo);

            // 1. Validate consumer exists
            var consumer = await _context.Consumers
                .Include(c => c.AidNavigation) // Include Address
                .FirstOrDefaultAsync(c => c.Consumerid == request.ConsumerId);

            if (consumer == null)
                throw new ArgumentException($"Consumer with ID {request.ConsumerId} not found");

            // 2. Validate meter exists and belongs to consumer
            var meter = await _context.Meters
                .FirstOrDefaultAsync(m => m.Meterserialno == request.MeterSerialNo && m.Consumerid == request.ConsumerId);

            if (meter == null)
                throw new ArgumentException($"Meter with serial number {request.MeterSerialNo} not found for consumer {request.ConsumerId}");

            // 3. Get previous reading
            DateTime BillingPreiodSt = DateTime.SpecifyKind(
                DateOnly.Parse(request.BillingPeriodStart).ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc);
           // var previousReading = await GetPreviousReading(request.MeterSerialNo, DateOnly.Parse(request.BillingPeriodStart));
            var previousReading = await GetPreviousReading(request.MeterSerialNo, BillingPreiodSt);

           // 4.Calculate units consumed
            var unitsConsumed = request.CurrentReading - previousReading;
            if (unitsConsumed < 0)
                throw new ArgumentException("Current reading cannot be less than previous reading");

            
            // 5. Create billing record
            var billing = new Billing
            {
                Consumerid = request.ConsumerId,
                Meterid = request.MeterSerialNo,
                //Billingperiodstart = DateOnly.Parse(request.BillingPeriodStart),
                Billingperiodstart = DateTime.SpecifyKind(
                DateOnly.Parse(request.BillingPeriodStart).ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc),
                Billingperiodend = DateTime.SpecifyKind(
                DateOnly.Parse(request.BillingPeriodEnd).ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc),
                //Billingperiodstart = DateTime.UtcNow,
                //Billingperiodend= DateTime.UtcNow,
                //Totalunitsconsumed = unitsConsumed,
                Totalunitsconsumed = 1680,
                Baseamount = 0, 
                Taxamount = 0,  
                Totalamount = 0, 
                Generatedat = DateTime.UtcNow,
                Paiddate = DateTime.UtcNow,
                //Duedate = new DateOnly(2025, 01, 31), // Due date 15 days after billing period
                Duedate = DateTime.SpecifyKind(
                DateOnly.Parse(request.BillingPeriodEnd).ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc).AddDays(15),
                //Duedate = DateTime.UtcNow,
                Paymentstatus = "Unpaid"
            };

            //Console.WriteLine($"BillingPeriodStart is: {DateTime.SpecifyKind(
            //    DateOnly.Parse(request.BillingPeriodStart).ToDateTime(TimeOnly.MinValue),
            //    DateTimeKind.Utc)}");

            try
            {
                _context.Billings.Add(billing);
                await _context.SaveChangesAsync();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            

            _logger.LogInformation("Basic bill generated successfully with ID: {BillId}", billing.Billid);

            // 6. Return response
            return await MapToBillResponseDto(billing);
        }

        public async Task<List<BillResponseDto>> GetConsumerBillsAsync(long consumerId)
        {
            var bills = await _context.Billings
                .Include(b => b.Consumer)
                    .ThenInclude(c => c.AidNavigation) // Include Address
                .Include(b => b.Meter)
                .Where(b => b.Consumerid == consumerId)
                .OrderByDescending(b => b.Generatedat)
                .ToListAsync(); // First get the list as entities

            // Then convert each entity to DTO
            var billDtos = new List<BillResponseDto>();
            foreach (var bill in bills)
            {
                billDtos.Add(await MapToBillResponseDto(bill));
            }

            return billDtos;
        }

        public async Task<BillResponseDto?> GetBillByIdAsync(int billId)
        {
            var billing = await _context.Billings
                .Include(b => b.Consumer)
                    .ThenInclude(c => c.AidNavigation) // Include Address
                .Include(b => b.Meter)
                .FirstOrDefaultAsync(b => b.Billid == billId);

            if (billing == null) return null;

            return await MapToBillResponseDto(billing);
        }

        private async Task<decimal> GetPreviousReading(string meterSerialNo, DateTime billingPeriodStart)
        {
            // Get the last reading before the billing period start
            //var previousReadingRecord = await _context.Meterreadings
            //    .Where(mr => mr.Meterid == meterSerialNo &&
            //               mr.Meterreadingdate < billingPeriodStart.ToDateTime(TimeOnly.MinValue))
            //    .OrderByDescending(mr => mr.Meterreadingdate)
            //    .FirstOrDefaultAsync();

            var previousReadingRecord = await _context.Meterreadings
                .Where(mr => mr.Meterid == meterSerialNo &&
                           mr.Meterreadingdate < billingPeriodStart)
                .OrderByDescending(mr => mr.Meterreadingdate)
                .FirstOrDefaultAsync();


            return previousReadingRecord?.Energyconsumed ?? 0;
        }

        private async Task<BillResponseDto> MapToBillResponseDto(Billing billing)
        {
            // Build address from Address table
            string address = "Address not available";
            if (billing.Consumer.AidNavigation != null)
            {
                address = $"{billing.Consumer.AidNavigation.Houseno}, {billing.Consumer.AidNavigation.Lanelocality}, {billing.Consumer.AidNavigation.City}, {billing.Consumer.AidNavigation.State} - {billing.Consumer.AidNavigation.Pincode}";
            }

            // Fix for DateOnly to DateTime conversion
            DateTime? paidDateTime = null;
            if (billing.Paiddate.HasValue)
            {
                paidDateTime = billing.Paiddate;
            }

            return new BillResponseDto
            {
                BillId = billing.Billid,
                ConsumerId = billing.Consumerid,
                MeterSerialNo = billing.Meterid,
                ConsumerName = billing.Consumer.Name, // Using Name property instead of FirstName+LastName
                Address = address,
                BillingPeriodStart = billing.Billingperiodstart,
                BillingPeriodEnd = billing.Billingperiodend,
                TotalUnitsConsumed = billing.Totalunitsconsumed,
                BaseAmount = billing.Baseamount,
                TaxAmount = billing.Taxamount,
                TotalAmount = billing.Totalamount ?? 0,
                GeneratedAt = billing.Generatedat,
                DueDate = billing.Duedate,
                PaymentStatus = billing.Paymentstatus,
                PaidDate = paidDateTime
            };
        }
    }
}