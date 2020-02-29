using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace WebhookDF.DAL
{
    public class CursoDAL
    {

        List<Models.Curso> _cursos = new List<Models.Curso>();

        public CursoDAL()
        {
            Models.Curso curso1 = new Models.Curso
            {
                Id = 1,
                Nome = "Sistemas para Internet",
                Preco = 200,
                Sinonimos = new List<string>() { "TSI", "Técnologo Sistemas para Internet" }
            };
            Models.Curso curso2 = new Models.Curso
            {
                Id = 2,
                Nome = "Ciência da Computação",
                Preco = 200,
                Sinonimos = new List<string>() { "BCC", "Bacharelado Ciência da Computação" }
            };
            Models.Curso curso3 = new Models.Curso
            {
                Id = 3,
                Nome = "Sistemas da Informação",
                Preco = 200,
                Sinonimos = new List<string>() { "BSI", "Bacharelado Sistemas da Informação" }
            };

            this._cursos.Add(curso1);
            this._cursos.Add(curso2);
            this._cursos.Add(curso3);
        }


        public IEnumerable<Models.Curso> ObterTodos()
        {
            return _cursos;
        }

        public string ObterTodosFormatoTexto() {

            string cursos = "\"" + string.Join("\",\"", (from curso in _cursos select curso.Nome).ToArray()) + "\"";
            return cursos;
        }

        public Models.Curso ObterCurso(string busca)
        {
            busca = busca.Trim().ToLower();

            Models.Curso curso = (from c in _cursos
                                  where c.Nome.ToLower() == busca || c.Sinonimos.Contains(busca)
                                  select c).FirstOrDefault();

            return curso;
        }

        public Models.Curso Obter(int Id)
        {
            Models.Curso curso = (from c in _cursos
                                  where c.Id == Id
                                  select c).FirstOrDefault();

            return curso;
        }

    }
}
