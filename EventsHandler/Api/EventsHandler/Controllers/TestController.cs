﻿// © 2024, Worth Systems.

using EventsHandler.Attributes.Authorization;
using EventsHandler.Attributes.Validation;
using EventsHandler.Controllers.Base;
using EventsHandler.Mapping.Enums;
using EventsHandler.Mapping.Models.POCOs.NotificatieApi;
using EventsHandler.Properties;
using EventsHandler.Services.DataProcessing.Enums;
using EventsHandler.Services.Register.Interfaces;
using EventsHandler.Services.Responding.Interfaces;
using EventsHandler.Services.Responding.Messages.Models.Errors;
using EventsHandler.Services.Serialization.Interfaces;
using EventsHandler.Services.Settings.Configuration;
using EventsHandler.Utilities.Swagger.Examples;
using Microsoft.AspNetCore.Mvc;
using Notify.Client;
using Notify.Models.Responses;
using Swashbuckle.AspNetCore.Filters;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace EventsHandler.Controllers
{
    /// <summary>
    /// Controller used to test other Web API services from which "Notify NL" OMC is dependent.
    /// </summary>
    /// <seealso cref="OmcController"/>
    public sealed class TestController : OmcController
    {
        private readonly WebApiConfiguration _configuration;
        private readonly ISerializationService _serializer;
        private readonly ITelemetryService _telemetry;
        private readonly IRespondingService<ProcessingResult, string> _responder;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestController"/> class.
        /// </summary>
        /// <param name="configuration">The service handling Data Provider (DAO) loading strategies.</param>
        /// <param name="serializer">The input de(serializing) service.</param>
        /// <param name="telemetry">The telemetry service registering API events.</param>
        /// <param name="responder">The output standardization service (UX/UI).</param>
        public TestController(
            WebApiConfiguration configuration,
            ISerializationService serializer,
            ITelemetryService telemetry,
            IRespondingService<ProcessingResult, string> responder)
        {
            this._configuration = configuration;
            this._serializer = serializer;
            this._telemetry = telemetry;
            this._responder = responder;
        }

        /// <summary>
        /// Checks the status of "Notify NL" Web API service.
        /// </summary>
        [HttpGet]
        [Route("Notify/HealthCheck")]
        // Security
        [ApiAuthorization]
        // User experience
        [StandardizeApiResponses]  // NOTE: Replace errors raised by ASP.NET Core with standardized API responses
        // Swagger UI
        [ProducesResponseType(StatusCodes.Status202Accepted)]                                                         // REASON: The API service is up and running
        [ProducesResponseType(StatusCodes.Status400BadRequest)]                                                       // REASON: The API service is currently down
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProcessingFailed.Simplified))]  // REASON: Unexpected internal error (if-else / try-catch-finally handle)
        public async Task<IActionResult> HealthCheckAsync()
        {
            try
            {
                // Health Check URL
                string healthCheckUrl = $"{this._configuration.OMC.API.BaseUrl.NotifyNL()}/_status?simple=true";

                // Request
                using HttpResponseMessage result = await new HttpClient().GetAsync(healthCheckUrl);

                // Response
                return result.IsSuccessStatusCode
                    // HttpStatus Code: 202 Accepted
                    ? LogApiResponse(LogLevel.Information,
                        this._responder.Get_Processing_Status_ActionResult(ProcessingResult.Success, result.ToString()))

                    // HttpStatus Code: 400 Bad Request
                    : LogApiResponse(LogLevel.Error,
                        this._responder.Get_Processing_Status_ActionResult(ProcessingResult.Failure, result.ToString()));
            }
            catch (Exception exception)
            {
                // HttpStatus Code: 500 Internal Server Error
                return LogApiResponse(exception,
                    this._responder.Get_Exception_ActionResult(exception));
            }
        }

        /// <summary>
        /// Sending Email messages to the "Notify NL" Web API service.
        /// </summary>
        /// <param name="emailAddress">The email address (required) where the notification should be sent.</param>
        /// <param name="emailTemplateId">The email template ID (optional) to be used from "Notify NL" API service.
        ///   <para>
        ///     If empty the ID of a very first looked up email template will be used.
        ///   </para>
        /// </param>
        /// <param name="personalization">The map (optional) of keys and values to be used as message personalization.
        ///   <para>
        ///     Example of personalization in template from "Notify NL" Admin Portal: "This is ((placeholderText)) information".
        ///   </para>
        ///   <para>
        ///     Example of personalization values to be provided: { "placeholderText": "good" }
        ///   </para>
        ///   <para>
        ///     Resulting message would be: "This is good information" (or exception, if personalization is required but not provided).
        ///   </para>
        /// </param>
        [HttpPost]
        [Route("Notify/SendEmail")]
        // Security
        [ApiAuthorization]
        // User experience
        [StandardizeApiResponses]  // NOTE: Replace errors raised by ASP.NET Core with standardized API responses
        // Swagger UI
        [SwaggerRequestExample(typeof(Dictionary<string, object>), typeof(PersonalizationExample))]
        [ProducesResponseType(StatusCodes.Status202Accepted)]                                                         // REASON: The notification successfully sent to "Notify NL" API service
        [ProducesResponseType(StatusCodes.Status400BadRequest,          Type = typeof(ProcessingFailed.Simplified))]  // REASON: Issues on the "Notify NL" API service side
        [ProducesResponseType(StatusCodes.Status403Forbidden,           Type = typeof(ProcessingFailed.Simplified))]  // REASON: Base URL or API key to "Notify NL" API service were incorrect
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ProcessingFailed.Simplified))]  // REASON: The JSON structure is invalid
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProcessingFailed.Simplified))]  // REASON: Unexpected internal error (if-else / try-catch-finally handle)
        [ProducesResponseType(StatusCodes.Status501NotImplemented,      Type = typeof(string))]                       // REASON: Operation is not implemented
        public async Task<IActionResult> SendEmailAsync(
            [Required, FromQuery] string emailAddress,
            [Optional, FromQuery] string? emailTemplateId,
            [Optional, FromBody] Dictionary<string, object> personalization)
        {
            return await SendAsync(
                NotifyMethods.Email,
                emailAddress,
                emailTemplateId,
                personalization);
        }

        /// <summary>
        /// Sending SMS text messages to the "Notify NL" Web API service.
        /// </summary>
        /// <param name="mobileNumber">The mobile phone number (required) where the notification should be sent.
        ///   <para>International country code is expected, e.g.: +1 (USA), +81 (Japan), +351 (Portugal), etc.</para>
        /// </param>
        /// <param name="smsTemplateId">The SMS template ID (optional) to be used from "Notify NL" API service.
        ///   <para>
        ///     If empty the ID of a very first looked up SMS template will be used.
        ///   </para>
        /// </param>
        /// <param name="personalization">
        ///   <inheritdoc cref="SendEmailAsync" path="/param[@name='personalization']"/>
        /// </param>
        [HttpPost]
        [Route("Notify/SendSms")]
        // Security
        [ApiAuthorization]
        // User experience
        [StandardizeApiResponses]  // NOTE: Replace errors raised by ASP.NET Core with standardized API responses
        // Swagger UI
        [SwaggerRequestExample(typeof(Dictionary<string, object>), typeof(PersonalizationExample))]
        [ProducesResponseType(StatusCodes.Status202Accepted)]                                                         // REASON: The notification successfully sent to "Notify NL" API service
        [ProducesResponseType(StatusCodes.Status400BadRequest,          Type = typeof(ProcessingFailed.Simplified))]  // REASON: Issues on the "Notify NL" API service side
        [ProducesResponseType(StatusCodes.Status403Forbidden,           Type = typeof(ProcessingFailed.Simplified))]  // REASON: Base URL or API key to "Notify NL" API service were incorrect
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ProcessingFailed.Simplified))]  // REASON: The JSON structure is invalid
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProcessingFailed.Simplified))]  // REASON: Unexpected internal error (if-else / try-catch-finally handle)
        [ProducesResponseType(StatusCodes.Status501NotImplemented,      Type = typeof(string))]                       // REASON: Operation is not implemented
        public async Task<IActionResult> SendSmsAsync(
            [Required, FromQuery] string mobileNumber,
            [Optional, FromQuery] string? smsTemplateId,
            [Optional, FromBody] Dictionary<string, object> personalization)
        {
            return await SendAsync(
            NotifyMethods.Sms,
                mobileNumber,
                smsTemplateId,
                personalization);
        }

        /// <summary>
        /// Checks whether feedback can be received by contact register Web API service.
        /// </summary>
        /// <param name="json">The notification from "OpenNotificaties" Web API service (as a plain JSON object).</param>
        [HttpPost]
        [Route("Open/ContactRegistration")]
        // Security
        [ApiAuthorization]
        // User experience
        [StandardizeApiResponses]  // NOTE: Replace errors raised by ASP.NET Core with standardized API responses
        [SwaggerRequestExample(typeof(NotificationEvent), typeof(NotificationEventExample))]  // NOTE: Documentation of expected JSON schema with sample and valid payload values
        [ProducesResponseType(StatusCodes.Status202Accepted)]                                                         // REASON: The registration was successfully sent to "Contactmomenten" API Web API service
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ProcessingFailed.Simplified))]  // REASON: The JSON structure is invalid
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProcessingFailed.Simplified))]  // REASON: The registration wasn't sent / Unexpected internal error (if-else / try-catch-finally handle)
        public async Task<IActionResult> RegisterAsync([Required, FromBody] object json)
        {
            try
            {
                NotificationEvent notification = this._serializer.Deserialize<NotificationEvent>(json);

                string result = await this._telemetry.ReportCompletionAsync(notification, NotifyMethods.Email, "test"); // TODO: Use notification method and message as parameters

                // HttpStatus Code: 202 Accepted
                return LogApiResponse(LogLevel.Information,
                    this._responder.Get_Processing_Status_ActionResult(ProcessingResult.Success, result));
            }
            catch (Exception exception)
            {
                // HttpStatus Code: 500 Internal Server Error
                return LogApiResponse(exception,
                    this._responder.Get_Exception_ActionResult(exception));
            }
        }

        #region Helper methods
        private static readonly Dictionary<NotifyMethods, string> s_templateTypes = new()
        {
            { NotifyMethods.Sms,   "sms"   },
            { NotifyMethods.Email, "email" }
        };

        /// <summary>
        /// Generic method sending notification through <see cref="NotificationClient"/> and handling its responses in a standardized way.
        /// </summary>
        /// <returns>
        ///   The standardized <see cref="ObjectResult"/> API response.
        /// </returns>
        private async Task<IActionResult> SendAsync(
            NotifyMethods notifyMethod,
            string contactDetails,
            string? templateId,
            Dictionary<string, object> personalization)
        {
            try
            {
                // Initialize the .NET client of "Notify NL" API service
                var notifyClient = new NotificationClient(  // TODO: Client to be resolved by IClientFactory (to be testable)
                    this._configuration.OMC.API.BaseUrl.NotifyNL().ToString(),
                    this._configuration.User.API.Key.NotifyNL());

                // Determine template type
                string templateType = s_templateTypes[notifyMethod];

                // Determine first possible Email template ID if nothing was provided
                List<TemplateResponse>? allTemplates = (await notifyClient.GetAllTemplatesAsync(templateType)).templates; // NOTE: Assign to variables for debug purposes
                templateId ??= allTemplates.First().id;

                // TODO: To be extracted into a dedicated service
                // NOTE: Empty personalization
                if (personalization.Count <= 1 &&
                    personalization.TryGetValue(PersonalizationExample.Key, out object? value) &&
                    Equals(value, PersonalizationExample.Value))
                {
                    switch (notifyMethod)
                    {
                        case NotifyMethods.Email:
                            _ = await notifyClient.SendEmailAsync(contactDetails, templateId);
                            break;

                        case NotifyMethods.Sms:
                            _ = await notifyClient.SendSmsAsync(contactDetails, templateId);
                            break;

                        default:
                            return LogApiResponse(LogLevel.Error,
                                this._responder.Get_Processing_Status_ActionResult(ProcessingResult.Failure, GetFailureMessage()));
                    }
                }
                // NOTE: Personalization was provided by the user
                else
                {
                    switch (notifyMethod)
                    {
                        case NotifyMethods.Email:
                            _ = await notifyClient.SendEmailAsync(contactDetails, templateId, personalization);
                            break;

                        case NotifyMethods.Sms:
                            _ = await notifyClient.SendSmsAsync(contactDetails, templateId, personalization);
                            break;

                        default:
                            return LogApiResponse(LogLevel.Error,
                                this._responder.Get_Processing_Status_ActionResult(ProcessingResult.Failure, GetFailureMessage()));
                    }
                }

                // HttpStatus Code: 202 Accepted
                return LogApiResponse(LogLevel.Information,
                    this._responder.Get_Processing_Status_ActionResult(ProcessingResult.Success, GetSuccessMessage(templateType)));
            }
            catch (Exception exception)
            {
                // HttpStatus Code: 500 Internal Server Error
                return LogApiResponse(exception,
                    this._responder.Get_Exception_ActionResult(exception));
            }
        }

        private static string GetFailureMessage()
            => Resources.Test_NotifyNL_ERROR_NotSupportedMethod;

        private static string GetSuccessMessage(string templateType)
            => $"The {templateType} {Resources.Test_NotifyNL_SUCCESS_NotificationSent}";
        #endregion
    }
}