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
        public List<string> Keys { get; set; }
        public List<string> Values { get; set; }

        public Sessao()
        {
            this.caminhoArquivoSessao = "candidatos.json";
            this.Values = new List<string>();
            this.Keys = new List<string>();
        }

        public void Recover()
        {
            StreamReader r = new StreamReader(this.caminhoArquivoSessao);

            string linha = "";
            string[] vetSessions;
            while((linha = r.ReadLine()) != null)
            {
                vetSessions = linha.Split(',');
                //apenas a primeira coluna da linha vai conter apenas a chave de sessão
                if (vetSessions.Length > 0 && vetSessions[0] == Id)
                {
                    for (int i = 1; i < vetSessions.Length; i+=1)
                    {
                        this.Keys.Add(vetSessions[i].Split('=')[0]);
                        this.Values.Add(vetSessions[i].Split('=')[1]);
                    }
                    break;
                }
            }

            r.Close();
        }

        public string Get(string key)
        {
            for (int i = 0; i < this.Keys.Count; i+=1)
            {
                if (this.Keys[i] == key)
                    return this.Values[i];
            }
            return "";
        }
        public void Save()
        {
            //Obtem as sessions ja gravadas, menos a atual
            StreamReader r = new StreamReader(this.caminhoArquivoSessao);
            string linha = "";
            string[] vet;
            string sessions = "";
            while ((linha = r.ReadLine()) != null)
            {
                vet = linha.Split(',');
                if (vet.Length > 0 && vet[0] != Id)
                    sessions += linha;
            }
            r.Close();
            //Concatena a session atual e grava as sessions
            StreamWriter w = new StreamWriter(this.caminhoArquivoSessao);
            string sessioPos = (this.Values.Count > 0 ? this.Id + "," : this.Id);
            for (int i = 0; i < this.Values.Count; i += 1)
            {
                if (i + 1 == this.Values.Count)
                    sessioPos += this.Keys[i] + "=" + this.Values[i];
                else
                    sessioPos += this.Keys[i] + "=" + this.Values[i] + ",";
            }
            w.WriteLine(sessioPos + sessions);
            w.Close();
        }

        public void Add(string key, string value)
        {
            bool exist = false;
            //verifica se existe
            for (int i = 0; i < this.Keys.Count; i += 1)
            {
                if (key == this.Keys[i])
                {
                    this.Values[i] = value;
                    exist = true;
                }
            }
            //se nao existe adiciona
            if(!exist)
            {
                this.Keys.Add(key);
                this.Values.Add(value);
            }
        }
        public void Clear()
        {
            StreamWriter w = new StreamWriter(this.caminhoArquivoSessao);
            w.Close();
        }
    }
}
