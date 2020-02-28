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
			return Ok(new { msg = "deu certo " +" "+_diretorio +  resposta });
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

							if (candidato != null)
							{
								//Salvando em sessão o cpf
								HttpContext.Session.SetInt32("cpfExists", 1);
								HttpContext.Session.SetString("cpf", candidato.CPF);
								response.FulfillmentText = "Olá " + candidato.Nome + ". Encontrei sua inscrição, Como posso te ajudar ?";
							}
							else
							{
								HttpContext.Session.SetInt32("cpfExists", 0);
								HttpContext.Session.SetString("cpf", cpf);
								response.FulfillmentText = "Não foi possível encontrar seus dados, qual o seu email?";
							}

						}
					}
					else if (action == "ActionInformaEmail")
					{
						//Recuperando da sessão o cpf se o candidato esta cadastrado
						string email = parameters.Fields["email"].StringValue;

						if (Convert.ToInt32(HttpContext.Session.GetInt32("cpfExists")) == 0 && candidato.EmailIsvalid(email))
						{
							HttpContext.Session.SetString("email", email);
							response.FulfillmentText = "Qual curso deseja?";
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
							response.FulfillmentText = "Vi que você não é um candidato. <a href=\"javascript: realizarInscricao();\">Clique aqui para se inscrever</a> ou me pergunte alguma coisa.";
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
							response.FulfillmentText = "Olá " + candidato.Nome + " sua inscrição foi realizada com sucesso!";
						else
							response.FulfillmentText = "Desculpe não foi possível realizar cadastro :(, por favor tente novamente mais tarde.";
					}
					else if (action == "ActionTesteWHPayload")
					{
						var contexto = request.QueryResult.OutputContexts;

						var payload = "{\"list\": {\"replacementKey\": \"@contexto\",\"invokeEvent\": true,\"afterDialog\": true,\"itemsName\": [\"Sim\",\"Não\"],\"itemsEventName\": [\"QueroInscrever\",\"NaoQueroInscrever\"]}}";


						response = new WebhookResponse()
						{
							FulfillmentText = "Teste Payload no WH com sucesso...",
							//Payload = Google.Protobuf.WellKnownTypes.Struct.Parser.ParseJson(payload)
							Payload = new Google.Protobuf.WellKnownTypes.Struct
							{
								Fields =
								{
									["postback"] = Value.ForString("Card Link URL or text"),
									["text"] = Value.ForString("Card Link Title")
								}
							}
						};


					}
				}
				catch (Exception ex)
				{

					response.FulfillmentText = "Erro: " + ex.Message.ToString();
				}
			}

			return Ok(response);


		}
	}
}
 