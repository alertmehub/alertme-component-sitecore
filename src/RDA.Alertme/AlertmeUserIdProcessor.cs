using System.Web.Security;
using Sitecore.Security.Accounts;

namespace RDA.AlertMe {
	public class AlertMeUserIdProcessor {
		public void Process(AlertMePipelineArgs args) {
			args.UserId = GetUserId();
		}

		private static string GetUserId() {
			User user = Sitecore.Context.User;
			MembershipUser membershipUser = Membership.GetUser(user.Name);
			object userId = membershipUser?.ProviderUserKey;

			return userId?.ToString().Replace("{", "").Replace("}", "");
		}
	}
}