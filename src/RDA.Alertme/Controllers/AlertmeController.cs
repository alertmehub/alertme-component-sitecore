using RDA.Alertme.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace RDA.Alertme.Controllers
{
	public class AlertmeController : Controller {
		public ActionResult Alertme() {
			AlertmeModel model = new AlertmeModel();
			const string errorMessage = "We're sorry. We are unable to load Alert Me at this time.";

			//Get settings item from Sitecore
			Sitecore.Data.Items.Item settingsItem;
			try {
				settingsItem = Sitecore.Context.Database.GetItem("/sitecore/system/modules/Alertme/Settings");
				Sitecore.Diagnostics.Assert.IsNotNull(settingsItem, "Settings item cannot be null.");
			}
			catch(Exception ex) {
				Sitecore.Diagnostics.Log.Error("Alertme: Unable to read settings item. It may have been moved or deleted.", this);
				model.ErrorMessage = errorMessage;
				return View(model);
			}

			//Read publisher id and api token from settings item
			string apiToken = string.Empty;
			string publisherId = string.Empty;
			try {
				apiToken = settingsItem.Fields["API Token"].Value;
				Sitecore.Diagnostics.Assert.IsNotNullOrEmpty(apiToken, "API Token must not be empty.");
				publisherId = settingsItem.Fields["Publisher Id"].Value;
				Sitecore.Diagnostics.Assert.IsNotNullOrEmpty(publisherId, "Publisher Id must not be empty.");
				model.APIToken = apiToken;
				model.PublisherId = publisherId;
			}
			catch(Exception ex) {
				Sitecore.Diagnostics.Log.Error("Alertme: Invalid value for API Token or Publisher Id in settings item.", this);
				model.ErrorMessage = errorMessage;
				return View(model);
			}

			//Run pipeline to get user id
			var args = new AlertmePipelineArgs();
			try {
				Sitecore.Pipelines.CorePipeline.Run("Alertme", args);
				model.CustomerId = args.UserId;
			}
			catch(Exception ex) {
				Sitecore.Diagnostics.Log.Error("Alertme: Unable to get the user id." + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace, this);
				model.ErrorMessage = errorMessage;
				return View(model);
			}

			//Get customer token
			string customerToken = string.Empty;
			try {
				customerToken = GetToken(apiToken, args.UserId);
				model.CustomerToken = customerToken;
			} catch (Exception ex) {
				Sitecore.Diagnostics.Log.Error("Alertme: There was an error getting the customer token." + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace, this);
				model.ErrorMessage = errorMessage;
				return View(model);
			}
			
			return View(model);
		}
        private string GetToken(string apiToken, string customerId)
        {
            string tokenUrl = "https://api.alertmehub.com/api/subscriber/token/v1/" + customerId;
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", apiToken);             
                string response = httpClient.GetStringAsync(tokenUrl).Result;
                return response.Replace("\"", "");
                
            }
        }
    }
}