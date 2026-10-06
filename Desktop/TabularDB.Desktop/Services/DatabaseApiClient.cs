using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TabularDB.Core.Contracts;
using TabularDB.Core.Domain;
using TabularDB.Desktop.Constants;

namespace TabularDB.Desktop.Services
{
    public sealed class DatabaseApiClient(HttpClient httpClient)
    {
        private static readonly JsonSerializerOptions _serializerOptions = CreateSerializerOptions();

        public async Task<List<TabularDatabase>> GetDatabasesAsync(CancellationToken cancellationToken = default)
        {
            return await GetAsync<List<TabularDatabase>>(ApiRoutes.DatabaseCollectionPath, cancellationToken);
        }

        public async Task<TabularDatabase> GetDatabaseAsync(Guid databaseId, CancellationToken cancellationToken = default)
        {
            return await GetAsync<TabularDatabase>(ApiRoutes.Database(databaseId), cancellationToken);
        }

        public async Task<TabularDatabase> CreateDatabaseAsync(string name, CancellationToken cancellationToken = default)
        {
            return await SendAsync<TabularDatabase>(HttpMethod.Post, ApiRoutes.DatabaseCollectionPath, new CreateDatabaseRequest(name), cancellationToken);
        }

        public Task DeleteDatabaseAsync(Guid databaseId, CancellationToken cancellationToken = default)
        {
            return SendWithoutResponseAsync(HttpMethod.Delete, ApiRoutes.Database(databaseId), null, cancellationToken);
        }

        public async Task<TabularTable> CreateTableAsync(Guid databaseId, string name, IEnumerable<ColumnModel> columns, CancellationToken cancellationToken = default)
        {
            return await SendAsync<TabularTable>(HttpMethod.Post, ApiRoutes.Tables(databaseId), new CreateTableRequest(name, [.. columns]), cancellationToken);
        }

        public Task DeleteTableAsync(Guid databaseId, Guid tableId, CancellationToken cancellationToken = default)
        {
            return SendWithoutResponseAsync(HttpMethod.Delete, ApiRoutes.Table(databaseId, tableId), null, cancellationToken);
        }

        public async Task<RowModel> CreateRowAsync(Guid databaseId, Guid tableId, IEnumerable<string> values, CancellationToken cancellationToken = default)
        {
            return await SendAsync<RowModel>(HttpMethod.Post, ApiRoutes.Rows(databaseId, tableId), new RowValuesRequest([.. values]), cancellationToken);
        }

        public async Task<RowModel> UpdateRowAsync(Guid databaseId, Guid tableId, Guid rowId, IEnumerable<string> values, CancellationToken cancellationToken = default)
        {
            return await SendAsync<RowModel>(HttpMethod.Put, ApiRoutes.Row(databaseId, tableId, rowId), new RowValuesRequest([.. values]), cancellationToken);
        }

        public Task DeleteRowAsync(Guid databaseId, Guid tableId, Guid rowId, CancellationToken cancellationToken = default)
        {
            return SendWithoutResponseAsync(HttpMethod.Delete, ApiRoutes.Row(databaseId, tableId, rowId), null, cancellationToken);
        }

        public Task RenameColumnAsync(Guid databaseId, Guid tableId, int columnIndex, string name, CancellationToken cancellationToken = default)
        {
            return SendWithoutResponseAsync(HttpMethod.Patch, ApiRoutes.ColumnName(databaseId, tableId, columnIndex), new RenameColumnRequest(name), cancellationToken);
        }

        public Task ReorderColumnAsync(Guid databaseId, Guid tableId, int fromIndex, int toIndex, CancellationToken cancellationToken = default)
        {
            return SendWithoutResponseAsync(HttpMethod.Patch, ApiRoutes.ColumnOrder(databaseId, tableId), new ReorderColumnRequest(fromIndex, toIndex), cancellationToken);
        }

        public async Task<TabularDatabase> ImportDatabaseAsync(TabularDatabase database, CancellationToken cancellationToken = default)
        {
            return await SendAsync<TabularDatabase>(HttpMethod.Post, ApiRoutes.DatabaseImportPath, database, cancellationToken);
        }

        public async Task<TabularDatabase> ExportDatabaseAsync(Guid databaseId, CancellationToken cancellationToken = default)
        {
            return await GetAsync<TabularDatabase>(ApiRoutes.Export(databaseId), cancellationToken);
        }

        private static JsonSerializerOptions CreateSerializerOptions()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }

        private async Task<TResponse> GetAsync<TResponse>(string requestUri, CancellationToken cancellationToken)
        {
            using var response = await httpClient.GetAsync(requestUri, cancellationToken);
            return await ReadResponseAsync<TResponse>(response, cancellationToken);
        }

        private async Task<TResponse> SendAsync<TResponse>(HttpMethod method, string requestUri, object? content, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(method, requestUri);
            if (content is not null)
            {
                request.Content = JsonContent.Create(content, options: _serializerOptions);
            }

            using var response = await httpClient.SendAsync(request, cancellationToken);
            return await ReadResponseAsync<TResponse>(response, cancellationToken);
        }

        private async Task SendWithoutResponseAsync(HttpMethod method, string requestUri, object? content, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(method, requestUri);

            if (content is not null)
            {
                request.Content = JsonContent.Create(content, options: _serializerOptions);
            }

            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw await CreateRequestExceptionAsync(response, cancellationToken);
            }
        }

        private static async Task<TResponse> ReadResponseAsync<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw await CreateRequestExceptionAsync(response, cancellationToken);
            }

            return await response.Content.ReadFromJsonAsync<TResponse>(_serializerOptions, cancellationToken)
                ?? throw new InvalidOperationException(ApiClientMessages.EmptyResponse);
        }

        private static async Task<HttpRequestException> CreateRequestExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            string message;

            try
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(_serializerOptions, cancellationToken);
                message = errorResponse?.Error ?? response.ReasonPhrase ?? ApiClientMessages.EmptyResponse;
            }
            catch (JsonException)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                message = string.IsNullOrWhiteSpace(responseBody)
                    ? response.ReasonPhrase ?? ApiClientMessages.EmptyResponse
                    : responseBody;
            }

            return new HttpRequestException(message, null, response.StatusCode);
        }
    }
}
