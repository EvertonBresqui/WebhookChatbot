using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Dialogflow.V2;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace WebhookDF.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebhookController : ControllerBase
    {
        private static readonly JsonParser _jsonParser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

        System.Text.Json.JsonSerializerOptions _jsonSetting = new System.Text.Json.JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };

        string _agentName = "botunoeste-tojago";
        string _diretorio = "";

        public WebhookController()
        {
        }


        [HttpGet]
        public IActionResult Get()
        {
            bool resposta = System.IO.File.Exists(_diretorio);
            return Ok(new { msg = "deu certo " + " " + _diretorio + resposta });
        }
        private bool Autorizado(IHeaderDictionary httpHeader)
        {

            string basicAuth = httpHeader["Authorization"];

            if (!string.IsNullOrEmpty(basicAuth))
            {
                basicAuth = basicAuth.Replace("Basic ", "");

                byte[] aux = System.Convert.FromBase64String(basicAuth);
                basicAuth = System.Text.Encoding.UTF8.GetString(aux);

                if (basicAuth == "nome:token")
                    return true;
            }

            return false;
        }

        [HttpPost("GetWebhookResponse")]
        public ActionResult GetWebhookResponse([FromBody] System.Text.Json.JsonElement dados)
        {
            if (!Autorizado(Request.Headers))
            {
                return StatusCode(401);
            }

            WebhookRequest request =
                _jsonParser.Parse<WebhookRequest>(dados.GetRawText());

            WebhookResponse response = new WebhookResponse();

            if (request != null)
            {
                string action = request.QueryResult.Action;
                var parameters = request.QueryResult.Parameters;
                try
                {
                    Models.Candidato candidato = new Models.Candidato();
                    candidato.CarregarBase();
                    Models.Curso curso = new Models.Curso();

                    if (action == "ActionInformaCPF")
                    {
                        var cpf = parameters.Fields["cpf"].StringValue;

                        if (cpf.Length > 11)
                        {
                            response.FulfillmentText = "CPF é inválido " + cpf;
                        }
                        else
                        {
                            //procurar CPF na base de dados
                            cpf = cpf.Replace(".", "");
                            cpf = cpf.Replace("-", "");
                            candidato = candidato.ObterCandidato(cpf);
                            this.Set("cpf", parameters.Fields["cpf"].StringValue, 1000);
                            //HttpContext.Session.SetString("cpf", parameters.Fields["cpf"].StringValue);

                            if (candidato != null)
                            {
                                //HttpContext.Session.SetInt32("logado", 1);
                                //HttpContext.Session.SetInt32("cpfExists", 1);
                                response.FulfillmentText = "Olá " + candidato.Nome + ". Encontrei sua inscrição, " + this.Menu();
                            }
                            else
                            {
                                //HttpContext.Session.SetInt32("cpfExists", 0);
                                response.FulfillmentText = "Não foi possível encontrar seus dados, qual o seu nome?";
                            }

                        }
                    }
                    else if (action == "ActionInformaNome")
                    {
                        //response.FulfillmentText = HttpContext.Session.GetString("cpf");
                        response.FulfillmentText = this.Get("cpf");
                        /*
                        HttpContext.Session.SetString("nome", parameters.Fields["nome"].StringValue);
                        response.FulfillmentText = "Qual o seu email?";*/
                    }
                    else if (action == "ActionInformaEmail")
                    {
                        //Recuperando da sessão o cpf se o candidato esta cadastrado
                        string email = parameters.Fields["email"].StringValue;

                        if (Convert.ToInt32(HttpContext.Session.GetInt32("cpfExists")) == 0 && candidato.EmailIsvalid(email))
                        {
                            HttpContext.Session.SetString("email", email);
                            var rcursos = curso.ObterTodos();
                            var mensagem = "Qual Curso Deseja ? <br/><ul>";

                            foreach (var item in rcursos)
                            {
                                mensagem += "<li><a href=\"javascript:BOT.InfCurso('" + item.Nome + "', '" + item.Url + "');\">" + item.Nome + "</a></li>";
                            }
                            mensagem += "</ul>";
                            response.FulfillmentText = mensagem;
                        }
                        else
                        {
                            response.FulfillmentText = "Informe um email válido!";
                        }
                    }
                    else if (action == "ActionInformaCurso")
                    {
                        string cursoo = parameters.Fields["curso"].StringValue;

                        curso = curso.Obter(cursoo);
                        if (curso != null)
                        {
                            HttpContext.Session.SetString("curso", curso.Nome);
                            response.FulfillmentText = "Vi que você não é um candidato. <a href=\"javascript: BOT.Gravar();\">Clique aqui para se inscrever</a> ou me pergunte alguma coisa.";
                        }
                        else
                        {
                            response.FulfillmentText = "Curso não encontrado!";
                        }
                    }
                    else if (action == "ActionCadastrar")
                    {
                        curso = curso.Obter(HttpContext.Session.GetString("curso"));
                        candidato.Setar(HttpContext.Session.GetString("nome"), HttpContext.Session.GetString("cpf"), HttpContext.Session.GetString("email"), curso);

                        if (candidato.Gravar())
                        {
                            HttpContext.Session.SetInt32("logado", 1);
                            response.FulfillmentText = "Olá " + candidato.Nome + " sua inscrição foi realizada com sucesso!";

                        }
                        else
                            response.FulfillmentText = "Desculpe não foi possível realizar cadastro :(, por favor tente novamente mais tarde.";
                    }
                    else if (action == "ActionMenu")
                    {
                        if (HttpContext.Session.GetInt32("logado") == 1)
                        {
                            response.FulfillmentText = this.Menu();
                        }
                    }
                    else if (action == "ActionObterDadosCadastrais")
                    {
                        if (HttpContext.Session.GetInt32("logado") == 1)
                        {
                            candidato = candidato.ObterCandidato(HttpContext.Session.GetString("cpf"));
                            response.FulfillmentText = "Informações cadastrais: <br/>" +
                                "Nome: " + candidato.Nome + "<br/>" +
                                "CPF: " + candidato.CPF + "<br/>" +
                                "Email:" + candidato.Email + " <br/>" +
                                "Vestibulando curso: " + candidato.Curso.Nome + " <br/>";
                        }
                    }
                    else if (action == "ActionObterResultadoVestibular")
                    {
                        if (HttpContext.Session.GetInt32("logado") == 1)
                        {
                            candidato = candidato.ObterCandidato(HttpContext.Session.GetString("cpf"));
                            if (candidato.ResVestibular == 1)
                                response.FulfillmentText = "Foi aprovado no vestibular :)";
                            else if (candidato.ResVestibular == 0)
                                response.FulfillmentText = "O resultado ainda não saiu :(";
                            else if (candidato.ResVestibular == -1)
                                response.FulfillmentText = "Infelizmente você foi reprovado na primeira chamada :(";
                        }
                    }
                    else if (action == "ActionObterNumeroAlunosMatriculados")
                    {
                        if (HttpContext.Session.GetInt32("logado") == 1)
                        {
                            candidato = candidato.ObterCandidato(HttpContext.Session.GetString("cpf"));
                            response.FulfillmentText = "O número de inscritos para o curso de " + candidato.Curso.Nome + " foi de " + candidato.Curso.NumeroInscritos + " incrições.";
                        }
                    }
                }
                catch (Exception ex)
                {

                    response.FulfillmentText = "Erro: " + ex.Message.ToString();
                }
            }

            return Ok(response);


        }

        private string Menu()
        {
            return "Quais informações deseja obter? <br/><ul>" +
                            "<li><a href=\"javascript:BOT.Menu(1);\">Obter dados cadastrais</a></li>" +
                            "<li><a href=\"javascript:BOT.Menu(2);\">Obter resultado vestibular</a></li>" +
                            "<li><a href=\"javascript:BOT.Menu(3);\">Número de alunos matriculados para este curso</a></li>" +
                            "<li><a href=\"javascript:BOT.Menu(4);\">Sobre a Unoeste</a></li>" +
                            "<li><a href=\"javascript:BOT.Menu(5);\">Quais cursos a Unoeste tem?</a></li></ul>";
        }
        /// <summary>  
        /// Get the cookie  
        /// </summary>  
        /// <param name="key">Key </param>  
        /// <returns>string value</returns>  
        public string Get(string key)
        {
            return Request.Cookies[key];
        }
        /// <summary>  
        /// set the cookie  
        /// </summary>  
        /// <param name="key">key (unique indentifier)</param>  
        /// <param name="value">value to store in cookie object</param>  
        /// <param name="expireTime">expiration time</param>  
        public void Set(string key, string value, int? expireTime)
        {
            CookieOptions option = new CookieOptions();
            if (expireTime.HasValue)
                option.Expires = DateTime.Now.AddMinutes(expireTime.Value);
            else
                option.Expires = DateTime.Now.AddMilliseconds(100);
            Response.Cookies.Append(key, value, option);
        }
    }
}
