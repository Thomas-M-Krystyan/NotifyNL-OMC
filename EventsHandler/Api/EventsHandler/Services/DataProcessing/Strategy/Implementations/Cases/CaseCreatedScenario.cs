﻿// © 2023, Worth Systems.

using EventsHandler.Mapping.Models.POCOs.NotificatieApi;
using EventsHandler.Mapping.Models.POCOs.OpenKlant;
using EventsHandler.Mapping.Models.POCOs.OpenZaak;
using EventsHandler.Services.DataProcessing.Strategy.Base;
using EventsHandler.Services.DataProcessing.Strategy.Base.Interfaces;
using EventsHandler.Services.DataProcessing.Strategy.Implementations.Cases.Base;
using EventsHandler.Services.DataProcessing.Strategy.Models.DTOs;
using EventsHandler.Services.DataQuerying.Adapter.Interfaces;
using EventsHandler.Services.DataQuerying.Interfaces;
using EventsHandler.Services.DataSending.Interfaces;
using EventsHandler.Services.Settings.Configuration;

namespace EventsHandler.Services.DataProcessing.Strategy.Implementations.Cases
{
    /// <summary>
    /// <inheritdoc cref="INotifyScenario"/>
    /// The strategy for "Case created" scenario.
    /// </summary>
    /// <seealso cref="BaseScenario"/>
    /// <seealso cref="BaseCaseScenario"/>
    internal sealed class CaseCreatedScenario : BaseCaseScenario
    {
        private IQueryContext _queryContext = null!;
        private CaseType _caseType;

        /// <summary>
        /// Initializes a new instance of the <see cref="CaseCreatedScenario"/> class.
        /// </summary>
        public CaseCreatedScenario(
            WebApiConfiguration configuration,
            IDataQueryService<NotificationEvent> dataQuery,
            INotifyService<NotificationEvent, NotifyData> notifyService)
            : base(configuration, dataQuery, notifyService)
        {
        }

        #region Polymorphic (PrepareDataAsync)
        /// <inheritdoc cref="BaseScenario.PrepareDataAsync(NotificationEvent)"/>
        protected override async Task<CommonPartyData> PrepareDataAsync(NotificationEvent notification)
        {
            // Setup
            this._queryContext = this.DataQuery.From(notification);
            
            this._caseType = await this._queryContext.GetLastCaseTypeAsync(     // 2. Case type (might be already cached)
                             await this._queryContext.GetCaseStatusesAsync());  // 1. Case statuses

            // Validation #1: The case type identifier must be whitelisted
            ValidateCaseId(
                this.Configuration.User.Whitelist.ZaakCreate_IDs().IsAllowed,
                this._caseType.Identification, GetWhitelistName());

            // Validation #2: The notifications must be enabled
            ValidateNotifyPermit(this._caseType.IsNotificationExpected);

            // Preparing citizen details
            return await this._queryContext.GetPartyDataAsync();
        }
        #endregion

        #region Polymorphic (Email logic: template + personalization)
        /// <inheritdoc cref="BaseScenario.GetEmailTemplateId()"/>
        protected override Guid GetEmailTemplateId()
            => this.Configuration.User.TemplateIds.Email.ZaakCreate();

        private static readonly object s_padlock = new();
        private static readonly Dictionary<string, object> s_emailPersonalization = new();  // Cached dictionary no need to be initialized every time
        /// <inheritdoc cref="BaseScenario.GetEmailPersonalizationAsync(CommonPartyData)"/>
        protected override async Task<Dictionary<string, object>> GetEmailPersonalizationAsync(CommonPartyData partyData)
        {
            Case @case = await this._queryContext.GetCaseAsync();

            lock (s_padlock)
            {
                s_emailPersonalization["klant.voornaam"] = partyData.Name;
                s_emailPersonalization["klant.voorvoegselAchternaam"] = partyData.SurnamePrefix;
                s_emailPersonalization["klant.achternaam"] = partyData.Surname;

                s_emailPersonalization["zaak.identificatie"] = @case.Identification;
                s_emailPersonalization["zaak.omschrijving"] = @case.Name;

                return s_emailPersonalization;
            }
        }
        #endregion

        #region Polymorphic (SMS logic: template + personalization)
        /// <inheritdoc cref="BaseScenario.GetSmsTemplateId()"/>
        protected override Guid GetSmsTemplateId()
          => this.Configuration.User.TemplateIds.Sms.ZaakCreate();

        /// <inheritdoc cref="BaseScenario.GetSmsPersonalizationAsync(CommonPartyData)"/>
        protected override async Task<Dictionary<string, object>> GetSmsPersonalizationAsync(CommonPartyData partyData)
        {
            return await GetEmailPersonalizationAsync(partyData);  // NOTE: Both implementations are identical
        }
        #endregion

        #region Polymorphic (GetWhitelistName)
        /// <inheritdoc cref="BaseScenario.GetWhitelistName()"/>
        protected override string GetWhitelistName() => this.Configuration.User.Whitelist.ZaakCreate_IDs().ToString();
        #endregion
    }
}