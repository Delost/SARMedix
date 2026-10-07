using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LoginSARMedix.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
        }


        public async Task<bool> EnviarCodigoRecuperacion(
            string correoDestino,
            string nombreUsuario,
            string codigo)
        {
            string? apiKey =
                _configuration["Brevo:ApiKey"];

            string? correoRemitente =
                _configuration["Brevo:RemitenteCorreo"];

            string? nombreRemitente =
                _configuration["Brevo:RemitenteNombre"];


            var contenidoCorreo = new
            {
                sender = new
                {
                    name = nombreRemitente,
                    email = correoRemitente
                },

                to = new[]
                {
                    new
                    {
                        email = correoDestino,
                        name = nombreUsuario
                    }
                },

                subject = "Código de recuperación SARMedix",

                htmlContent =
                    $@"
                    <html>
                        <body>

                            <h2>
                                Recuperación de contraseña
                            </h2>

                            <p>
                                Hola {nombreUsuario}.
                            </p>

                            <p>
                                Tu código de recuperación es:
                            </p>

                            <h1>
                                {codigo}
                            </h1>

                            <p>
                                Este código vence en 10 minutos.
                            </p>

                            <p>
                                Si no solicitaste este cambio,
                                puedes ignorar este correo.
                            </p>

                            <br>

                            <p>
                                SARMedix
                                <br>
                                SAR de Chiguay
                            </p>

                        </body>
                    </html>"
            };


            string json =
                JsonSerializer.Serialize(
                    contenidoCorreo
                );


            using HttpRequestMessage request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://api.brevo.com/v3/smtp/email"
                );


            request.Headers.Add(
                "api-key",
                apiKey
            );


            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/json"
                )
            );


            request.Content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                );


            HttpResponseMessage response =
      await _httpClient.SendAsync(
          request
      );


            string respuestaBrevo =
                await response.Content
                    .ReadAsStringAsync();


            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    "ERROR BREVO: "
                    + respuestaBrevo
                );
            }


            return true;
        }
    }
}