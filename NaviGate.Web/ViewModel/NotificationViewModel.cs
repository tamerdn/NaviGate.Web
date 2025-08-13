using NaviGate.Web.Models;
using System.Collections.Generic;

namespace NaviGate.Web.ViewModels
{
    public class NotificationViewModel
    {
        public int UnresolvedCount { get; set; }
        public List<AiAlert> RecentUnresolvedAlerts { get; set; }

        public NotificationViewModel()
        {
            RecentUnresolvedAlerts = new List<AiAlert>();
        }
    }
}
