﻿// © 2024, Worth Systems.

namespace EventsHandler.Services.DataLoading.Interfaces
{
    /// <summary>
    /// The service responsible for loading a specific data from an associated resource.
    /// <para>
    ///   It works similarly to Data Access Object (DAO) structure.
    /// </para>
    /// </summary>
    public interface ILoadingService
    {
        /// <summary>
        /// Loads a generic type of data using the dedicated <see langword="string"/> key.
        /// </summary>
        /// <typeparam name="TData">The generic type of data to be returned.</typeparam>
        /// <param name="key">The key to be used to check up a specific value.</param>
        /// <returns>
        ///   The generic data value associated with the key.
        /// </returns>
        /// <exception cref="KeyNotFoundException">The provided key is missing or invalid.</exception>
        internal TData GetData<TData>(string key);

        /// <summary>
        /// Precedes the (eventually formatted) node name with a respective separator.
        /// </summary>
        /// <param name="nodeName">The name of the configuration node.</param>
        /// <returns>
        ///   The formatted node path.
        /// </returns>
        internal string GetNodePath(string nodeName);
    }
}
