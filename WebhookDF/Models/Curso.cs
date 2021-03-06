﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebhookDF.Models
{
    public class Curso
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public double Preco {get;set;}
        public string Url { get; set; }
        public int NumeroInscritos { get; set; }
        public List<string> Sinonimos { get; set; }
        public Curso()
        {
            Sinonimos = new List<string>();
        }

        public Models.Curso Obter(string curso){
            DAL.CursoDAL cursoDal = new DAL.CursoDAL();
            return cursoDal.ObterCurso(curso);
        }

        public List<Curso> ObterTodos()
        {
            DAL.CursoDAL cursoDal = new DAL.CursoDAL();
            return cursoDal.ObterTodos();
        }


    }
}
