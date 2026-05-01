namespace Vittal.Aplicacion.Models
{
    public class ApiResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ApiResponse<T> : ApiResponse
    {
        public T? Data { get; set; }
    }
}
