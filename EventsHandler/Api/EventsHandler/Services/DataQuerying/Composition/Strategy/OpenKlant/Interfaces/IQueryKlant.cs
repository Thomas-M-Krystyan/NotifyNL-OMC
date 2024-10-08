﻿// © 2024, Worth Systems.

using EventsHandler.Exceptions;
using EventsHandler.Mapping.Models.POCOs.OpenKlant;
using EventsHandler.Services.DataQuerying.Composition.Interfaces;
using EventsHandler.Services.Settings.Configuration;
using EventsHandler.Services.Versioning.Interfaces;
using System.Text.Json;

namespace EventsHandler.Services.DataQuerying.Composition.Strategy.OpenKlant.Interfaces
{
    /// <summary>
    /// The methods querying specific data from "OpenKlant" Web API service.
    /// </summary>
    /// <seealso cref="IVersionDetails"/>
    internal interface IQueryKlant : IVersionDetails, IDomain
    {
        /// <inheritdoc cref="WebApiConfiguration"/>
        protected internal WebApiConfiguration Configuration { get; set; }

        /// <inheritdoc cref="IVersionDetails.Name"/>
        string IVersionDetails.Name => "OpenKlant";

        #region Abstract (Citizen details)
        /// <summary>
        /// Gets the details of a specific citizen from "OpenKlant" Web API service.
        /// </summary>
        /// <param name="queryBase"><inheritdoc cref="IQueryBase" path="/summary"/></param>
        /// <param name="openKlantDomain">The domain of <see cref="IQueryKlant"/> Web API service.</param>
        /// <param name="bsnNumber">The BSN (Citizen Service Number).</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="KeyNotFoundException"/>
        /// <exception cref="HttpRequestException"/>
        /// <exception cref="JsonException"/>
        internal Task<CommonPartyData> TryGetPartyDataAsync(IQueryBase queryBase, string openKlantDomain, string bsnNumber);
        #endregion

        #region Abstract (Telemetry)
        /// <summary>
        /// Sends the completion feedback to "OpenKlant" Web API service.
        /// </summary>
        /// <param name="queryBase"><inheritdoc cref="IQueryBase" path="/summary"/></param>
        /// <param name="openKlantDomain">The domain of <see cref="IQueryKlant"/> Web API service.</param>
        /// <param name="jsonBody">The content in JSON format to be passed with POST request as HTTP Request Body.</param>
        /// <exception cref="KeyNotFoundException"/>
        /// <exception cref="TelemetryException"/>
        /// <exception cref="JsonException"/>
        internal Task<ContactMoment> SendFeedbackAsync(IQueryBase queryBase, string openKlantDomain, string jsonBody);
        #endregion

        #region Polymorphic (Domain)
        /// <inheritdoc cref="IDomain.GetDomain"/>
        string IDomain.GetDomain() => this.Configuration.User.Domain.OpenKlant();
        #endregion
    }
}