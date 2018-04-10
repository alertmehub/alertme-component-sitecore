namespace RDA.Alertme {
	public class AlertmeUserIdProcessor {
		public void Process(AlertmePipelineArgs args) {
			args.UserId = GetUserId();
		}

		private string GetUserId() {
			var user = Sitecore.Context.User;
			var membershipUser = System.Web.Security.Membership.GetUser(user.Name);
			var userId = membershipUser.ProviderUserKey;

			return userId.ToString().Replace("{", "").Replace("}", "");
		}
	}
}