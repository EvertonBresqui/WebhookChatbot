using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;
using Grpc.Core;
using Microsoft.AspNetCore.Hosting;
using Google.Protobuf;

namespace WebhookDF.DAL
{
    public class CandidatoDAL
    {
        private List<Models.Candidato> _candidatos = new List<Models.Candidato>();

        public CandidatoDAL()
        {
            DAL.CursoDAL cursoDal = new CursoDAL();
            Models.Candidato candidato1 = new Models.Candidato()
            {
                Id = 1,
                Nome = "Everton",
                ResVestibular = 1,
                CPF = "45094431889",
                Curso = cursoDal.Obter(1),
                Email = "everton_bresqui@hotmail.com"
            };
            Models.Candidato candidato2 = new Models.Candidato()
            {
                Id = 2,
                Nome = "Jean",
                ResVestibular = 1,
                CPF = "44444444444",
                Curso = cursoDal.Obter(1),
                Email = "jean@hotmail.com"
            };
            Models.Candidato candidato3 = new Models.Candidato()
            {
                Id = 3,
                Nome = "Vinicius",
                ResVestibular = 1,
                CPF = "55555555555",
                Curso = cursoDal.Obter(3),
                Email = "vinicius@hotmail.com"
            };
            this._candidatos.Add(candidato1);
            this._candidatos.Add(candidato2);
            this._candidatos.Add(candidato3);
        }


        public IEnumerable<Models.Candidato> ObterTodos()
        {
            return _candidatos;
        }

  
        public Models.Candidato ObterCandidato(string cpf)
        {
            cpf = cpf.Trim().ToLower();

            Models.Candidato candidato = (from c in _candidatos
                                          where c.CPF == cpf
                                          select c).FirstOrDefault();

            return candidato;
        }

        public void Gravar(Models.Candidato candidato){
            this._candidatos.Add(candidato);
        }
    }
}
