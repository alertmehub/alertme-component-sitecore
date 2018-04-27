using System;
using System.Net.Http;
using System.Web.Mvc;
using RDA.AlertMe.Models;
using Sitecore.Mvc.Controllers;

namespace RDA.AlertMe.Controllers {
    public class AlertMeController : SitecoreController {
        public ActionResult AlertMe() {
            const string errorMessage = "We're sorry. We are unable to load Alert Me at this time.";
            AlertMeModel model = new AlertMeModel();
            Sitecore.Data.Items.Item settingsItem;

            //Get settings item from Sitecore
            try {
                settingsItem = Sitecore.Context.Database.GetItem(Constants.Modules.Settings.Id);
                Sitecore.Diagnostics.Assert.IsNotNull(settingsItem, "Settings item cannot be null.");
            } catch (Exception ex) {
                Sitecore.Diagnostics.Log.Error("Alertme: Unable to read settings item. It may have been moved or deleted.", ex, this);
                model.ErrorMessage = errorMessage;
                return View(model);
            }

            //Read publisher id and api token from settings item
            try {
                string apiToken = settingsItem.Fields[Constants.Modules.Settings.ApiToken].Value;
                Sitecore.Diagnostics.Assert.IsNotNullOrEmpty(apiToken, "API Token must not be empty.");

                string publisherId = settingsItem.Fields[Constants.Modules.Settings.PublisherId].Value;
                Sitecore.Diagnostics.Assert.IsNotNullOrEmpty(publisherId, "Publisher Id must not be empty.");

                model.ApiToken = apiToken;
                model.PublisherId = publisherId;
            } catch (Exception ex) {
                Sitecore.Diagnostics.Log.Error("Alertme: Invalid value for API Token or Publisher Id in settings item.", ex, this);
                model.ErrorMessage = errorMessage;
                return View(model);
            }

            //Run pipeline to get user id
            try {
                AlertMePipelineArgs args = new AlertMePipelineArgs();
                Sitecore.Pipelines.CorePipeline.Run("Alertme", args);
                model.CustomerId = args.UserId;
            } catch (Exception ex) {
                Sitecore.Diagnostics.Log.Error($"Alertme: Unable to get the user id.{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}", this);
                model.ErrorMessage = errorMessage;
                return View(model);
            }

            //Get customer token
            try {
                string customerToken = GetToken(model.ApiToken, model.CustomerId);
                model.CustomerToken = customerToken;
            } catch (Exception ex) {
                Sitecore.Diagnostics.Log.Error($"Alertme: There was an error getting the customer token.{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}", this);
                model.ErrorMessage = errorMessage;
                return View(model);
            }

            return View(model);
        }

        private static string GetToken(string apiToken, string customerId) {
            string tokenUrl = "https://api.alertmehub.com/api/v1/subscriber/token/" + customerId;

            using (HttpClient httpClient = new HttpClient()) {
                httpClient.DefaultRequestHeaders.Add("Authorization", apiToken);
                string response = httpClient.GetStringAsync(tokenUrl).Result;
                return response.Replace("\"", "");
            }
        }
    }
}