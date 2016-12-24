using Microsoft.Extensions.Configuration;

namespace App.Models
{
    public class Threshold
    {
        public int UserLockThreshold { get; private set; }
        public int IpBanThreshold { get; private set; }

        public Threshold(IConfiguration config)
        {
            UserLockThreshold = int.TryParse(config["ISU4_USER_LOCK_THRESHOLD"], out var userLockThreshold) ? userLockThreshold : 3;
            IpBanThreshold = int.TryParse(config["ISU4_IP_BAN_THRESHOLD"], out var ipBanThreshold) ? ipBanThreshold : 10;
        }

        public Threshold(int userLockThreshold, int ipBanThreshold)
        {
            UserLockThreshold = userLockThreshold;
            IpBanThreshold = ipBanThreshold;
        }
    }
}
