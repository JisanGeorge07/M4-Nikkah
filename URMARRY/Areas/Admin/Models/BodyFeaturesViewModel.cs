using Application.Models;

namespace URMARRY.Areas.Admin.Models
{
    public class BodyFeaturesViewModel
    {
        public IList<BodyFeaturesDto>? BodyFeatures { get; set; }
        public string Type { get; set; }
    }
}
