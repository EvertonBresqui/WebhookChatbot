﻿using System;
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
        public Utils.Sessao Sessao { get; set; }

        public WebhookController()
        {
            this.Sessao = new Utils.Sessao();
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
                //Obtem o id da sesão do dialogflow
                this.Sessao.Id = this.getIdSession(request.QueryResult.OutputContexts[0].Name);
                this.Sessao.Recover();

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
                            //gravao cpf na sessao
                            this.Sessao.Add("cpf", cpf);

                            if (candidato != null)
                            {
                                this.Sessao.Add("logado", "1");
                                response.FulfillmentText = "Olá " + candidato.Nome + ". Encontrei sua inscrição, " + this.Menu();
                            }
                            else
                            {
                                this.Sessao.Add("logado", "0");
                                response.FulfillmentText = "Não foi possível encontrar seus dados, qual o seu nome?";
                            }
                            this.Sessao.Save();
                        }
                    }
                    else if (action == "ActionInformaNome")
                    {
                        if (this.Sessao.Get("logado") == "0")
                        {
                            this.Sessao.Add("nome", parameters.Fields["nome"].StringValue);
                            this.Sessao.Save();
                            response.FulfillmentText = "Qual o seu email?";
                        }
                    }
                    else if (action == "ActionInformaEmail")
                    {
                        if (this.Sessao.Get("logado") == "0")
                        {
                            string email = parameters.Fields["email"].StringValue;

                            if (candidato.EmailIsvalid(email))
                            {
                                this.Sessao.Add("email", email);
                                this.Sessao.Save();
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
                    }
                    else if (action == "ActionInformaCurso")
                    {
                        curso = curso.Obter(parameters.Fields["curso"].StringValue);
                        if (curso != null)
                        {
                            this.Sessao.Add("curso", curso.Nome);
                            this.Sessao.Save();
                            response.FulfillmentText = "Vi que você não é um candidato. <a href=\"javascript: BOT.Gravar();\">Clique aqui para se inscrever</a> ou me pergunte alguma coisa.";
                        }
                        else
                        {
                            response.FulfillmentText = "Curso não encontrado!";
                        }
                    }
                    else if (action == "ActionCadastrar")
                    {
                        curso = curso.Obter(this.Sessao.Get("curso"));
                        candidato.Setar(this.Sessao.Get("nome"), this.Sessao.Get("cpf"), this.Sessao.Get("email"), curso);

                        if (candidato.Gravar())
                        {
                            this.Sessao.Add("logado", "1");
                            response.FulfillmentText = "Olá " + candidato.Nome + " sua inscrição foi realizada com sucesso!" + this.Menu();

                        }
                        else
                            response.FulfillmentText = "Desculpe não foi possível realizar cadastro :(, por favor tente novamente mais tarde.";
                    }
                    else if (action == "ActionMenu")
                    {
                        if (this.Sessao.Get("logado") == "1")
                        {
                            response.FulfillmentText = this.Menu();
                        }
                    }
                    else if (action == "ActionObterDadosCadastrais")
                    {
                        if (this.Sessao.Get("logado") == "1")
                        {
                            candidato = candidato.ObterCandidato(this.Sessao.Get("cpf"));
                            response.FulfillmentText = "Informações cadastrais: <br/>" +
                                "Nome: " + candidato.Nome + "<br/>" +
                                "CPF: " + candidato.CPF + "<br/>" +
                                "Email:" + candidato.Email + " <br/>" +
                                "Vestibulando curso: " + candidato.Curso.Nome + " <br/>";
                        }
                    }
                    else if (action == "ActionObterResultadoVestibular")
                    {
                        if (this.Sessao.Get("logado") == "1")
                        {
                            candidato = candidato.ObterCandidato(this.Sessao.Get("cpf"));
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
                        if (this.Sessao.Get("logado") == "1")
                        {
                            candidato = candidato.ObterCandidato(this.Sessao.Get("cpf"));
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


        public string getIdSession(string name)
        {
            if (name != "" && name.Contains("/"))
                return name.Split('/')[4];
            return "";
        }
        private string Menu()
        {
            return "Quais informações deseja obter? <br/><ul>" +
                            "<li><a href=\"javascript:BOT.Menu(1);\">Ir para área do candidato.</a></li>" +
                            "<li><a href=\"javascript:BOT.Menu(2);\">Sobre a Unoeste</a></li>" +
                            "<li><a href=\"javascript:BOT.Menu(3);\">Quais cursos a Unoeste tem?</a></li></ul>";
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
