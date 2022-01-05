using ICD.Connect.Conferencing.DirectoryMiddlewareClient.Model;

namespace ICD.Connect.Conferencing.DirectoryMiddlewareClient.Requests
{
	public class SearchRequest
	{
		public int Page { get; set; }
		public int PageSize { get; set; }
		public string Query { get; set; }
		public eSearchFilter Filter { get; set; }
	}
}
