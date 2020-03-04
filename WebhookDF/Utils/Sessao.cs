using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace WebhookDF.Utils
{
    public class Sessao
    {
        private string caminhoArquivoSessao;
        public string Id { get; set; }
        public List<string> Valores { get; set; }

        public Sessao()
        {
            this.caminhoArquivoSessao = "candidatos.json";
            this.Valores = new List<string>();
        }

        public void RecuperarSessao(string Id)
        {
            StreamReader r = new StreamReader(this.caminhoArquivoSessao);

            string linha = "";
            string[] vet;
            while(linha != null)
            {
                linha = r.ReadLine();
                vet = linha.Split(',');
                if(vet.Length > 0 && vet[0] == Id)
                    this.Valores = linha.Split(',').ToList();
            }

            r.Close();
        }

        public void GravarSessao()
        {
            StreamWriter w = File.AppendText(this.caminhoArquivoSessao);
            string linha = "";
            for (int i = 0; i < this.Valores.Count; i+=1)
            {
                if (i + 1 == this.Valores.Count)
                    linha += this.Valores[i];
                else
                    linha += this.Valores[i] + ",";
            }
            w.WriteLine(linha);
            w.Close();
        }
    }
}
