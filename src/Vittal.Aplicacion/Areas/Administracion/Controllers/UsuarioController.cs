using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

using Vittal.Aplicacion.Helpers;



namespace Vittal.Aplicacion.Areas.Administracion.Controllers

{

    /// <summary>

    /// DTO interno para recibir datos del formulario de usuarios desde el cliente.

    /// </summary>

    public class UsuarioFormDto

    {

        public string Username { get; set; } = string.Empty;

        public string Nombres { get; set; } = string.Empty;

        public string Apellidos { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Password { get; set; }

        public Guid PerfilId { get; set; }

        public string? Sexo { get; set; }

        public string? Celular { get; set; }

        public string? Direccion { get; set; }

        public bool EsDoctor { get; set; }

    }



    [Area("Administracion")]

    [Authorize]

    public class UsuarioController : Controller

    {

        private readonly ApiClientHelper _apiClient;

        private readonly ILogger<UsuarioController> _logger;



        public UsuarioController(ApiClientHelper apiClient, ILogger<UsuarioController> logger)

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

            var (success, response, _) = await _apiClient.GetAsync<JsonElement>($"api/Usuarios/{id}");



            if (!success)

            {

                TempData["Error"] = "Error al conectar con la API.";

                return RedirectToAction("Index");

            }



            var data = ExtractDataObject(response);

            if (data == null)

            {

                TempData["Error"] = "Usuario no encontrado.";

                return RedirectToAction("Index");

            }



            ViewBag.Usuario = data;

            return View();

        }



        // ===================== JSON PROXY ENDPOINTS (para JavaScript) =====================



        /// <summary>Lista todos los usuarios (activos por defecto) -- para fetch() desde la vista Index</summary>

        [HttpGet]

        public async Task<IActionResult> JsonUsuarios([FromQuery] bool inactivos = false)

        {

            var url = inactivos ? "api/Usuarios?inactivos=true" : "api/Usuarios";

            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>(url);



            if (!success)

            {

                _logger.LogWarning("JsonUsuarios API call failed: {Error}", errorMessage);

                return Json(new { success = false, message = errorMessage ?? "Error al cargar usuarios" });

            }



            var data = ExtractDataArray(response);

            return Json(new { success = true, data = data });

        }



        /// <summary>Crea un nuevo usuario -- para fetch() desde la vista Create</summary>

        [HttpPost]

        public async Task<IActionResult> JsonCrear([FromBody] UsuarioFormDto dto)

        {

            if (string.IsNullOrWhiteSpace(dto.Username))

            {

                return BadRequest(new { success = false, message = "El nombre de usuario es obligatorio." });

            }



            if (string.IsNullOrWhiteSpace(dto.Nombres))

            {

                return BadRequest(new { success = false, message = "Los nombres son obligatorios." });

            }



            if (string.IsNullOrWhiteSpace(dto.Apellidos))

            {

                return BadRequest(new { success = false, message = "Los apellidos son obligatorios." });

            }



            if (string.IsNullOrWhiteSpace(dto.Email))

            {

                return BadRequest(new { success = false, message = "El correo electronico es obligatorio." });

            }



            if (string.IsNullOrWhiteSpace(dto.Password))

            {

                return BadRequest(new { success = false, message = "La contraseña es obligatoria." });

            }



            if (dto.PerfilId == Guid.Empty)

            {

                return BadRequest(new { success = false, message = "Debe seleccionar un perfil." });

            }



            _logger.LogInformation("JsonCrear called: username={Username}, email={Email}", dto.Username, dto.Email);



            var (success, response, errorMessage) = await _apiClient.PostAsync<JsonElement>("api/Usuarios",

                new

                {

                    username = dto.Username,

                    nombres = dto.Nombres,

                    apellidos = dto.Apellidos,

                    email = dto.Email,

                    password = dto.Password,

                    perfilId = dto.PerfilId,

                    sexo = dto.Sexo,

                    celular = dto.Celular,

                    direccion = dto.Direccion,

                    esDoctor = dto.EsDoctor

                });



            if (!success)

            {

                _logger.LogWarning("JsonCrear API call failed: {Error}", errorMessage);

                return BadRequest(new { success = false, message = errorMessage ?? "Error al crear usuario" });

            }



            var data = ExtractDataObject(response);

            return Ok(new { success = true, data = data, message = "Usuario creado exitosamente" });

        }



        /// <summary>Actualiza un usuario -- para fetch() desde la vista Edit</summary>

        [HttpPut]

        public async Task<IActionResult> JsonActualizar(Guid id, [FromBody] UsuarioFormDto dto)

        {

            if (string.IsNullOrWhiteSpace(dto.Username))

            {

                return BadRequest(new { success = false, message = "El nombre de usuario es obligatorio." });

            }



            if (string.IsNullOrWhiteSpace(dto.Nombres))

            {

                return BadRequest(new { success = false, message = "Los nombres son obligatorios." });

            }



            if (string.IsNullOrWhiteSpace(dto.Apellidos))

            {

                return BadRequest(new { success = false, message = "Los apellidos son obligatorios." });

            }



            if (string.IsNullOrWhiteSpace(dto.Email))

            {

                return BadRequest(new { success = false, message = "El correo electronico es obligatorio." });

            }



            if (dto.PerfilId == Guid.Empty)

            {

                return BadRequest(new { success = false, message = "Debe seleccionar un perfil." });

            }



            _logger.LogInformation("JsonActualizar called: id={Id}, username={Username}", id, dto.Username);



            var payload = new

            {

                username = dto.Username,

                nombres = dto.Nombres,

                apellidos = dto.Apellidos,

                email = dto.Email,

                perfilId = dto.PerfilId,

                sexo = dto.Sexo,

                celular = dto.Celular,

                direccion = dto.Direccion,

                esDoctor = dto.EsDoctor

            };



            // Only include password if provided

            var payloadDict = new Dictionary<string, object?>

            {

                ["username"] = dto.Username,

                ["nombres"] = dto.Nombres,

                ["apellidos"] = dto.Apellidos,

                ["email"] = dto.Email,

                ["perfilId"] = dto.PerfilId,

                ["sexo"] = dto.Sexo ?? string.Empty,

                ["celular"] = dto.Celular ?? string.Empty,

                ["direccion"] = dto.Direccion ?? string.Empty,

                ["esDoctor"] = dto.EsDoctor

            };



            if (!string.IsNullOrWhiteSpace(dto.Password))

            {

                payloadDict["password"] = dto.Password;

            }



            var (success, response, errorMessage) = await _apiClient.PutAsync<JsonElement>($"api/Usuarios/{id}", payloadDict);



            if (!success)

            {

                _logger.LogWarning("JsonActualizar API call failed: {Error}", errorMessage);

                return BadRequest(new { success = false, message = errorMessage ?? "Error al actualizar usuario" });

            }



            var data = ExtractDataObject(response);

            return Ok(new { success = true, data = data, message = "Usuario actualizado exitosamente" });

        }



        /// <summary>Desactiva un usuario -- para fetch() desde la vista Index</summary>

        [HttpPatch]

        public async Task<IActionResult> JsonDesactivar(Guid id)

        {

            _logger.LogInformation("JsonDesactivar called: id={Id}", id);



            var (success, _, errorMessage) = await _apiClient.PatchAsync<JsonElement>($"api/Usuarios/{id}/desactivar", null);



            if (!success)

            {

                _logger.LogWarning("JsonDesactivar API call failed: {Error}", errorMessage);

                return BadRequest(new { success = false, message = errorMessage ?? "Error al desactivar usuario" });

            }



            return Ok(new { success = true, message = "Usuario desactivado exitosamente" });

        }



        /// <summary>Reactiva un usuario -- para fetch() desde la vista Index</summary>

        [HttpPatch]

        public async Task<IActionResult> JsonReactivar(Guid id)

        {

            _logger.LogInformation("JsonReactivar called: id={Id}", id);



            var (success, _, errorMessage) = await _apiClient.PatchAsync<JsonElement>($"api/Usuarios/{id}/reactivar", null);



            if (!success)

            {

                _logger.LogWarning("JsonReactivar API call failed: {Error}", errorMessage);

                return BadRequest(new { success = false, message = errorMessage ?? "Error al reactivar usuario" });

            }



            return Ok(new { success = true, message = "Usuario reactivado exitosamente" });

        }



        /// <summary>Lista doctores -- para dropdowns en otras vistas</summary>

        [HttpGet]

        public async Task<IActionResult> JsonDoctores()

        {

            var (success, response, errorMessage) = await _apiClient.GetAsync<JsonElement>("api/Usuarios/doctores");



            if (!success)

            {

                _logger.LogWarning("JsonDoctores API call failed: {Error}", errorMessage);

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

            catch { /* return empty */ }



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

    }

}

