﻿// © 2024, Worth Systems.

using EventsHandler.Behaviors.Mapping.Models.Interfaces;
using System.Text.Json.Serialization;

namespace EventsHandler.Behaviors.Mapping.Models.POCOs.OpenKlant.v2
{
    /// <summary>
    /// The results about the parties (e.g., citizen, organization) retrieved from "OpenKlant" Web service.
    /// </summary>
    /// <remarks>
    ///   Version: "OpenKlant" (2.0) Web service.
    /// </remarks>
    /// <seealso cref="IJsonSerializable"/>
    public struct PartyResults : IJsonSerializable
    {
        /// <summary>
        /// The number of received results.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("count")]
        [JsonPropertyOrder(0)]
        public int Count { get; internal set; }

        /// <inheritdoc cref="PartyResult"/>
        [JsonInclude]
        [JsonPropertyName("results")]
        [JsonPropertyOrder(1)]
        public List<PartyResult> Results { get; internal set; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="PartyResults"/> struct.
        /// </summary>
        public PartyResults()
        {
        }

        /// <summary>
        /// Gets the <see cref="PartyResult"/>.
        /// </summary>
        /// <value>
        ///   The data of a single party.
        /// </value>
        internal readonly PartyResult Party()
        {
            throw new NotImplementedException();
        }
    }
}