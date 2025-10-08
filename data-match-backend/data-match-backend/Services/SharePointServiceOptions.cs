
namespace DataMatchBackend.Services
{
    public class SharePointServiceOptions
    {
        public string? SiteUrl { get; set; }
        public string? OpportunityListTitle { get; set; } = "Opportunity List";
        public string SalePersonListTitle { get; set; } = "Sale Persons";
    }
}