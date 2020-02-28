using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace WebhookDF.Models
{
    public class Candidato
    {
        int _id;
        string _nome, _CPF, _curso;

        public int Id { get; set; }
        public string Nome { get; set; }
        public Models.Curso Curso { get; set; }
        public string CPF { get; set; }
        public string Email { get; set; }
        private DAL.CandidatoDAL dalCandidato;

        public Models.Candidato ObterCandidato(string cpf){
            this.dalCandidato = new DAL.CandidatoDAL();
            return dalCandidato.ObterCandidato(cpf);
        }

        public bool EmailIsvalid(string email){
            Regex rg = new Regex(@"^[A-Za-z0-9](([_\.\-]?[a-zA-Z0-9]+)*)@([A-Za-z0-9]+)(([\.\-]?[a-zA-Z0-9]+)*)\.([A-Za-z]{2,})$");
            if (rg.IsMatch(email))
                return true;
            return false;
        }

        public void Setar(string nome, string cpf, string email, Models.Curso curso){
            this.Nome = nome;
            this.CPF = cpf;
            this.Email = email;
            this.Curso = curso;
        }

        public bool Gravar(){
            this.dalCandidato.Gravar(this);
            return true;
        }

        public void CarregarBase()
        {
            this.dalCandidato = new DAL.CandidatoDAL();
        }
    }
}
