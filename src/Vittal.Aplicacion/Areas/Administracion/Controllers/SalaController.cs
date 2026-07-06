using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

using Vittal.Aplicacion.Helpers;



namespace Vittal.Aplicacion.Areas.Administracion.Controllers

{

    /// <summary>

    /// DTO interno para recibir datos del formulario de salas desde el cliente.

    /// </summary>

    public class SalaFormDto

    {

        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

    }

    /// <summary>

    /// DTO interno para aplicar plantilla desde el formulario.

    /// </summary>

    public class AplicarPlantillaFormDto

    {

        public Guid SalaId { get; set; }

        public Guid PlantillaId { get; set; }

    }

    /// <summary>
    /// DTO interno para crear/editar un tipo de antecedente desde la vista Details.
    /// </summary>
    public class TipoAntecedenteFormDto
    {
        public Guid SalaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Categoria { get; set; }
        public string TipoDato { get; set; } = "boolean";
        public int Orden { get; set; }
    }

    /// <summary>
    /// DTO interno para crear/editar un tipo de signo vital desde la vista Details.
    /// </summary>
    public class TipoSignoVitalFormDto
    {
        public Guid SalaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Unidad { get; set; }
        public decimal? ValorMin { get; set; }
        public decimal? ValorMax { get; set; }
        public int Orden { get; set; }
        public bool EsObligatorio { get; set; }
    }

    /// <summary>
    /// ViewModel para la vista unificada de detalle de Sala (Fase 4).
    /// </summary>
    public class SalaDetailsViewModel
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }
        public string? FechaCreacion { get; set; }
        public string? FechaModificacion { get; set; }

        /// <summary>JSON en serie para los tipos de antecedente (cargados via API).</summary>
        public string AntecedentesJson { get; set; } = "[]";
        /// <summary>JSON en serie para los tipos de signo vital (cargados via API).</summary>
        public string SignosVitalesJson { get; set; } = "[]";
        /// <summary>JSON en serie para las plantillas disponibles.</summary>
        public string PlantillasJson { get; set; } = "[]";
        /// <summary>JSON en serie para los doctores asignados.</summary>
        public string DoctoresJson { get; set; } = "[]";
    }

    [Area("Administracion")]

    [Authorize]

    public class SalaController : Controller

    {

        private readonly ApiClientHelper _apiClient;

        private readonly ILogger<SalaController> _logger;



        public SalaController(ApiClientHelper apiClient, ILogger<SalaController> logger)

        {

            _apiClient = apiClient;

            _logger = logger;

        }

        // ===================== VISTAS (Server-side rendering) =====================

        [HttpGet]

        public IActionResult Index()

        {

            return View();

        }

        [HttpGet]

        public IActionResult Create()

        {

            return View();

        }

        [HttpGet]

        public async Task<IActionResult> Edit(Guid id)

        {

            var (success, response, _) = await _apiClient.GetAsync<JsonElement>($"api/Salas/{id}");

            if (!success)

            {

                TempData["Error"] = "Error al conectar con la API.";

                return RedirectToAction("Index");

            }

            var data = ExtractDataObject(response);

            if (data == null)

            {

                TempData["Error"] = "Sala no encontrada.";

                return RedirectToAction("Index");

            }

            ViewBag.Sala = data;

            return View();

        }

        // ===================== VISTA UNIFICADA DE SALA (Fase 4) =====================

        /// <summary>
        /// Vista unificada de detalle de Sala.
        /// Muestra info de la sala, antecedentes, signos vitales, doctores asignados
        /// y selector de plantilla de especialidad en una sola página.
        /// </summary>
        [HttpGet]

        public async Task<IActionResult> Details(Guid id)

        {

            var model = new SalaDetailsViewModel();

            // 1. Datos de la sala
            var (success, response, _) = await _apiClient.GetAsync<JsonElement>($"api/Salas/{id}");
            if (!success)
            {
                TempData["Error"] = "Error al conectar con la API.";
                return RedirectToAction("Index");
            }

            var salaDict = ExtractDataObject(response) as Dictionary<string, object?>;
            if (salaDict == null)
            {
                TempData["Error"] = "Sala no encontrada.";
                return RedirectToAction("Index");
            }

            model.Id = GetGuid(salaDict, "id");
            model.Nombre = GetString(salaDict, "nombre");
            model.Descripcion = GetString(salaDict, "descripcion");
            model.Activo = GetBool(salaDict, "activo");
            model.FechaCreacion = GetString(salaDict, "fechaCreacion");
            model.FechaModificacion = GetString(salaDict, "fechaModificacion");

            // 2. Tipos de Antecedente
            model.AntecedentesJson = await FetchJsonArrayAsync($"api/TipoAntecedente/sala/{id}");

            // 3. Tipos de Signo Vital
            model.SignosVitalesJson = await FetchJsonArrayAsync($"api/TipoSignoVital/sala/{id}");

            // 4. Plantillas de Especialidad disponibles
            model.PlantillasJson = await FetchJsonArrayAsync("api/PlantillaEspecialidad");

            // 5. Doctores asignados a la sala
            model.DoctoresJson = await FetchJsonArrayAsync($"api/UsuariosSalas/sala/{id}");

            return View(model);

        }

        // ===================== JSON PROXY ENDPOINTS (para JavaScript) =====================

        /// <summary>Lista todas las salas - para fetch() desde la vista Index</summary>

        [HttpGet]

        public async Task<IActionResult> JsonSalas([FromQuery] bool inactivos = false)

        {

            var url = inactivos ? "api/Salas?inactivos=true" : "api/Salas";

            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>(url);

            if (!success)

            {

                _logger.LogWarning("JsonSalas API call failed: {Error}", errorMessage);

                return Json(new { success = false, message = errorMessage ?? "Error al cargar salas" });

            }

            var data = ExtractDataArray(response);

            return Json(new { success = true, data = data });

        }

        /// <summary>Crea una nueva sala - para fetch() desde la vista Create</summary>

        [HttpPost]

        public async Task<IActionResult> JsonCrear([FromBody] SalaFormDto dto)

        {

            if (string.IsNullOrWhiteSpace(dto.Nombre))

            {

                return BadRequest(new { success = false, message = "El nombre es obligatorio." });

            }

            _logger.LogInformation("JsonCrear called: nombre={Nombre}", dto.Nombre);

            var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/Salas",

                new { nombre = dto.Nombre, descripcion = dto.Descripcion });

            if (!success)

            {

                _logger.LogWarning("JsonCrear API call failed: {Error}", errorMessage);

                return BadRequest(new { success = false, message = errorMessage ?? "Error al crear sala" });

            }

            var data = ExtractDataObject(response);

            return Ok(new { success = true, data = data, message = "Sala creada exitosamente" });

        }

        /// <summary>Actualiza una sala - para fetch() desde la vista Edit</summary>

        [HttpPut]

        public async Task<IActionResult> JsonActualizar(Guid id, [FromBody] SalaFormDto dto)

        {

            if (string.IsNullOrWhiteSpace(dto.Nombre))

            {

                return BadRequest(new { success = false, message = "El nombre es obligatorio." });

            }

            _logger.LogInformation("JsonActualizar called: id={Id}, nombre={Nombre}", id, dto.Nombre);

            var (success, response, errorMessage) = await _apiClient.PutAsync<JsonElement>($"api/Salas/{id}",

                new { nombre = dto.Nombre, descripcion = dto.Descripcion });

            if (!success)

            {

                _logger.LogWarning("JsonActualizar API call failed: {Error}", errorMessage);

                return BadRequest(new { success = false, message = errorMessage ?? "Error al actualizar sala" });

            }

            var data = ExtractDataObject(response);

            return Ok(new { success = true, data = data, message = "Sala actualizada exitosamente" });

        }

        /// <summary>Desactiva una sala - para fetch() desde la vista Index</summary>

        [HttpPatch]

        public async Task<IActionResult> JsonDesactivar(Guid id)

        {

            _logger.LogInformation("JsonDesactivar called: id={Id}", id);

            var (success, _, errorMessage) = await _apiClient.PatchAsync<JsonElement>($"api/Salas/{id}/desactivar", null);

            if (!success)

            {

                _logger.LogWarning("JsonDesactivar API call failed: {Error}", errorMessage);

                return BadRequest(new { success = false, message = errorMessage ?? "Error al desactivar sala" });

            }

            return Ok(new { success = true, message = "Sala desactivada exitosamente" });

        }

        /// <summary>Reactiva una sala - para fetch() desde la vista Index</summary>

        [HttpPatch]

        public async Task<IActionResult> JsonReactivar(Guid id)

        {

            var (success, _, errorMessage) = await _apiClient.PatchAsync<JsonElement>($"api/Salas/{id}/reactivar", null);

            if (!success)

            {

                return BadRequest(new { success = false, message = errorMessage ?? "Error al reactivar sala" });

            }

            return Ok(new { success = true, message = "Sala reactivada exitosamente" });

        }

        // ===================== PROXIES PARA PLANTILLAS DE ESPECIALIDAD =====================

        /// <summary>Lista las plantillas de especialidad disponibles - para dropdown en Edit</summary>

        [HttpGet]

        public async Task<IActionResult> JsonListarPlantillas()

        {

            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/PlantillaEspecialidad");

            if (!success)

            {

                _logger.LogWarning("JsonListarPlantillas API call failed: {Error}", errorMessage);

                return Json(new { success = false, message = errorMessage ?? "Error al cargar plantillas" });

            }

            var data = ExtractDataArray(response);

            return Json(new { success = true, data = data });

        }

        /// <summary>Aplica una plantilla de especialidad a una sala - HU-E02</summary>

        [HttpPost]

        public async Task<IActionResult> JsonAplicarPlantilla([FromBody] AplicarPlantillaFormDto dto)

        {

            if (dto.SalaId == Guid.Empty || dto.PlantillaId == Guid.Empty)

            {

                return BadRequest(new { success = false, message = "Debe especificar sala y plantilla." });

            }

            _logger.LogInformation("JsonAplicarPlantilla called: salaId={SalaId}, plantillaId={PlantillaId}",

                dto.SalaId, dto.PlantillaId);

            var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>(

                $"api/Salas/{dto.SalaId}/aplicar-plantilla/{dto.PlantillaId}", null);

            if (!success)

            {

                _logger.LogWarning("JsonAplicarPlantilla API call failed: {Error}", errorMessage);

                return BadRequest(new { success = false, message = errorMessage ?? "Error al aplicar plantilla" });

            }

            var data = ExtractDataObject(response);

            return Ok(new { success = true, data = data, message = "Plantilla aplicada exitosamente" });

        }

        // ===================== PROXIES PARA TIPOS DE ANTECEDENTE =====================

        /// <summary>Lista los tipos de antecedente de una sala - para fetch() desde la vista Details</summary>
        [HttpGet]

        public async Task<IActionResult> JsonAntecedentes([FromQuery] Guid salaId)

        {

            if (salaId == Guid.Empty)

                return Json(new { success = false, message = "SalaId es obligatorio." });

            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/TipoAntecedente/sala/{salaId}");

            if (!success)

            {

                _logger.LogWarning("JsonAntecedentes API call failed: {Error}", errorMessage);

                return Json(new { success = false, message = errorMessage ?? "Error al cargar antecedentes" });

            }

            var data = ExtractDataArray(response);

            return Json(new { success = true, data = data });

        }

        /// <summary>Crea un tipo de antecedente - para fetch() desde la vista Details</summary>
        [HttpPost]

        public async Task<IActionResult> JsonCrearAntecedente([FromBody] TipoAntecedenteFormDto dto)

        {

            if (string.IsNullOrWhiteSpace(dto.Nombre))

                return BadRequest(new { success = false, message = "El nombre es obligatorio." });

            var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/TipoAntecedente",
                new { salaId = dto.SalaId, nombre = dto.Nombre, categoria = dto.Categoria, tipoDato = dto.TipoDato, orden = dto.Orden });

            if (!success)

            {

                _logger.LogWarning("JsonCrearAntecedente API call failed: {Error}", errorMessage);

                return BadRequest(new { success = false, message = errorMessage ?? "Error al crear antecedente" });

            }

            return Ok(new { success = true, message = "Antecedente creado exitosamente" });

        }

        /// <summary>Actualiza un tipo de antecedente - para fetch() desde la vista Details</summary>
        [HttpPut]

        public async Task<IActionResult> JsonActualizarAntecedente(Guid id, [FromBody] TipoAntecedenteFormDto dto)

        {

            if (string.IsNullOrWhiteSpace(dto.Nombre))

                return BadRequest(new { success = false, message = "El nombre es obligatorio." });

            var (success, response, errorMessage) = await _apiClient.PutAsync<JsonElement>($"api/TipoAntecedente/{id}",
                new { salaId = dto.SalaId, nombre = dto.Nombre, categoria = dto.Categoria, tipoDato = dto.TipoDato, orden = dto.Orden });

            if (!success)

            {

                _logger.LogWarning("JsonActualizarAntecedente API call failed: {Error}", errorMessage);

                return BadRequest(new { success = false, message = errorMessage ?? "Error al actualizar antecedente" });

            }

            return Ok(new { success = true, message = "Antecedente actualizado exitosamente" });

        }

        /// <summary>Desactiva un tipo de antecedente - para fetch() desde la vista Details</summary>
        [HttpPatch]

        public async Task<IActionResult> JsonDesactivarAntecedente(Guid id)

        {

            var (success, _, errorMessage) = await _apiClient.PatchAsync<JsonElement>($"api/TipoAntecedente/{id}/desactivar", null);

            if (!success)

            {

                _logger.LogWarning("JsonDesactivarAntecedente API call failed: {Error}", errorMessage);

                return BadRequest(new { success = false, message = errorMessage ?? "Error al desactivar antecedente" });

            }

            return Ok(new { success = true, message = "Antecedente desactivado exitosamente" });

        }

        // ===================== PROXIES PARA TIPOS DE SIGNO VITAL =====================

        /// <summary>Lista los tipos de signo vital de una sala - para fetch() desde la vista Details</summary>
        [HttpGet]

        public async Task<IActionResult> JsonSignosVitales([FromQuery] Guid salaId)

        {

            if (salaId == Guid.Empty)

                return Json(new { success = false, message = "SalaId es obligatorio." });

            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/TipoSignoVital/sala/{salaId}");

            if (!success)

            {

                _logger.LogWarning("JsonSignosVitales API call failed: {Error}", errorMessage);

                return Json(new { success = false, message = errorMessage ?? "Error al cargar signos vitales" });

            }

            var data = ExtractDataArray(response);

            return Json(new { success = true, data = data });

        }

        /// <summary>Crea un tipo de signo vital - para fetch() desde la vista Details</summary>
        [HttpPost]

        public async Task<IActionResult> JsonCrearSignoVital([FromBody] TipoSignoVitalFormDto dto)

        {

            if (string.IsNullOrWhiteSpace(dto.Nombre))

                return BadRequest(new { success = false, message = "El nombre es obligatorio." });

            var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/TipoSignoVital",
                new { salaId = dto.SalaId, nombre = dto.Nombre, unidad = dto.Unidad, valorMin = dto.ValorMin, valorMax = dto.ValorMax, orden = dto.Orden, esObligatorio = dto.EsObligatorio });

            if (!success)

            {

                _logger.LogWarning("JsonCrearSignoVital API call failed: {Error}", errorMessage);

                return BadRequest(new { success = false, message = errorMessage ?? "Error al crear signo vital" });

            }

            return Ok(new { success = true, message = "Signo vital creado exitosamente" });

        }

        /// <summary>Actualiza un tipo de signo vital - para fetch() desde la vista Details</summary>
        [HttpPut]

        public async Task<IActionResult> JsonActualizarSignoVital(Guid id, [FromBody] TipoSignoVitalFormDto dto)

        {

            if (string.IsNullOrWhiteSpace(dto.Nombre))

                return BadRequest(new { success = false, message = "El nombre es obligatorio." });

            var (success, response, errorMessage) = await _apiClient.PutAsync<JsonElement>($"api/TipoSignoVital/{id}",
                new { salaId = dto.SalaId, nombre = dto.Nombre, unidad = dto.Unidad, valorMin = dto.ValorMin, valorMax = dto.ValorMax, orden = dto.Orden, esObligatorio = dto.EsObligatorio });

            if (!success)

            {

                _logger.LogWarning("JsonActualizarSignoVital API call failed: {Error}", errorMessage);

                return BadRequest(new { success = false, message = errorMessage ?? "Error al actualizar signo vital" });

            }

            return Ok(new { success = true, message = "Signo vital actualizado exitosamente" });

        }

        /// <summary>Desactiva un tipo de signo vital - para fetch() desde la vista Details</summary>
        [HttpPatch]

        public async Task<IActionResult> JsonDesactivarSignoVital(Guid id)

        {

            var (success, _, errorMessage) = await _apiClient.PatchAsync<JsonElement>($"api/TipoSignoVital/{id}/desactivar", null);

            if (!success)

            {

                _logger.LogWarning("JsonDesactivarSignoVital API call failed: {Error}", errorMessage);

                return BadRequest(new { success = false, message = errorMessage ?? "Error al desactivar signo vital" });

            }

            return Ok(new { success = true, message = "Signo vital desactivado exitosamente" });

        }

        // ===================== PROXY PARA DOCTORES ASIGNADOS =====================

        /// <summary>Lista los doctores asignados a una sala - para fetch() desde la vista Details</summary>
        [HttpGet]

        public async Task<IActionResult> JsonDoctoresSala([FromQuery] Guid salaId)

        {

            if (salaId == Guid.Empty)

                return Json(new { success = false, message = "SalaId es obligatorio." });

            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>($"api/UsuariosSalas/sala/{salaId}");

            if (!success)

            {

                _logger.LogWarning("JsonDoctoresSala API call failed: {Error}", errorMessage);

                return Json(new { success = false, message = errorMessage ?? "Error al cargar doctores" });

            }

            var data = ExtractDataArray(response);

            return Json(new { success = true, data = data });

        }

        // ========== Helpers para extraer data de JsonElement ==========

        private static IEnumerable<object> ExtractDataArray(JsonElement? response)

        {

            if (!response.HasValue) return new List<object>();

            try

            {

                if (response.Value.TryGetProperty("data", out var dataProp))

                {

                    return EnumerateJsonArray(dataProp);

                }

                if (response.Value.ValueKind == JsonValueKind.Array)

                {

                    return EnumerateJsonArray(response.Value);

                }

            }

            catch { }

            return new List<object>();

        }

        private static object? ExtractDataObject(JsonElement? response)

        {

            if (!response.HasValue) return null;

            try

            {

                if (response.Value.TryGetProperty("data", out var dataProp))

                {

                    return JsonElementToDictionary(dataProp);

                }

                return JsonElementToDictionary(response.Value);

            }

            catch { return null; }

        }

        private static IEnumerable<object> EnumerateJsonArray(JsonElement array)

        {

            var list = new List<object>();

            foreach (var item in array.EnumerateArray())

            {

                list.Add(JsonElementToDictionary(item));

            }

            return list;

        }

        private static Dictionary<string, object?> JsonElementToDictionary(JsonElement element)

        {

            var dict = new Dictionary<string, object?>();

            if (element.ValueKind == JsonValueKind.Object)

            {

                foreach (var prop in element.EnumerateObject())

                {

                    dict[prop.Name] = JsonElementToValue(prop.Value);

                }

            }

            return dict;

        }

        private static object? JsonElementToValue(JsonElement element)

        {

            return element.ValueKind switch

            {

                JsonValueKind.String => element.GetString(),

                JsonValueKind.Number => element.GetDouble(),

                JsonValueKind.True => true,

                JsonValueKind.False => false,

                JsonValueKind.Null => null,

                JsonValueKind.Object => JsonElementToDictionary(element),

                JsonValueKind.Array => EnumerateJsonArray(element).ToList(),

                _ => element.GetRawText()

            };

        }

        // ========== Helpers para SalaDetailsViewModel ==========

        private static Guid GetGuid(Dictionary<string, object?> dict, string key)

        {

            if (dict.TryGetValue(key, out var val) && val != null)

            {

                if (val is Guid guid) return guid;

                if (Guid.TryParse(val.ToString(), out var parsed)) return parsed;

            }

            return Guid.Empty;

        }

        private static string GetString(Dictionary<string, object?> dict, string key)

        {

            if (dict.TryGetValue(key, out var val) && val != null)

                return val.ToString() ?? string.Empty;

            return string.Empty;

        }

        private static bool GetBool(Dictionary<string, object?> dict, string key)

        {

            if (dict.TryGetValue(key, out var val) && val != null)

            {

                if (val is bool b) return b;

                if (bool.TryParse(val.ToString(), out var parsed)) return parsed;

            }

            return false;

        }

        /// <summary>
        /// Obtiene datos desde una URL de la API y los retorna como JSON array string.
        /// Si falla o no hay datos, retorna "[]".
        /// </summary>
        private async Task<string> FetchJsonArrayAsync(string url)

        {

            try

            {

                var (success, response, _) = await _apiClient.GetAsync<JsonElement>(url);

                if (success)

                {

                    var data = ExtractDataArray(response);

                    return System.Text.Json.JsonSerializer.Serialize(data);

                }

            }

            catch (Exception ex)

            {

                _logger.LogWarning(ex, "FetchJsonArrayAsync falló para {Url}", url);

            }

            return "[]";

        }

    }

}
