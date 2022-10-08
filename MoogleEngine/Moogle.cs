using System.Runtime.CompilerServices;
using System.Linq;
namespace MoogleEngine;


public static class Moogle
{
    public static SearchResult Query(string search)
    {
        string address = "../Content";

        dataBase_Manage BD = new dataBase_Manage(address);

        //calcular los tf-idf
        VectorialModel.tfxidf_Result(BD);

        //crear la query y calcular sus tf-idf
        Doc query = new Doc(search);

        //sugerencia
        string suggestion = SetSuggestion(query, BD.total_words);

        if (ValidQuery(query, BD.total_words))
        {
            //calcular tf-idf de la query
            VectorialModel.tfxidf_Result(BD, query);

            //calcular los scores
            VectorialModel.SimCos(BD, query);

            //ordenar los scores
            BD.documents = BD.documents.OrderByDescending(doc => doc.score).ToList();

            //crear la snippet de cada documento
            SetSnippets(query, BD);

            if (BD.documents.All(doc => doc.score == 0))
            {
                SearchItem[] item = new SearchItem[1] { new SearchItem("ERROR", "Not Found", 0) };
                return new SearchResult(item, suggestion);

            }

            //
            //TRABAJAR CON LOS RESULTADOSSSSSS
            List<SearchItem> results = new List<SearchItem>();
            foreach (var doc in BD.documents)
            {
                if (doc.score != 0)
                {
                    results.Add(new SearchItem(doc.title, doc.Snippet, doc.score));
                }
            }

            SearchItem[] items = results.ToArray();
            return new SearchResult(items, suggestion);
        }
        else
        {
            SearchItem[] items = new SearchItem[1] { new SearchItem("ERROR", "Not Found", 0) };
            return new SearchResult(items, suggestion);
        }
    }


    private static bool ValidQuery(Doc query, Dictionary<string, float> total_words)
    {
        foreach (var word in query.words_Snippet)
        {
            if (total_words.ContainsKey(word))
            {
                return true;
            }
        }
        return false;
    }
    private static void SetSnippets(Doc query, dataBase_Manage BD)
    {
        int large = 100;
        for (int i = 0; i < BD.documents.Count; i++)
        {
            if (BD.documents[i].score > 0)
            {
                if (large > BD.documents[i].words_Snippet.Length)
                {
                    foreach (var word in BD.documents[i].text)
                    {
                        BD.documents[i].Snippet += " " + word;
                    }
                }
                else
                {
                    GetSnippet(query, BD, i, large);
                }
            }

        }
    }
    private static void GetSnippet(Doc query, dataBase_Manage BD, int index, int large)
    {
        int position = -1;
        float max = 0;
        float frecuency;
        position = -1;

        //buscar en todas las posibles snippets la mejor
        for (int i = 0; i + large < BD.documents[index].words_Snippet.Length; i++)
        {
            frecuency = 0;
            foreach (var word in query.TFxIDF)
            {
                if (BD.documents[index].words_Snippet[i..(i + large)].Contains(word.Key) && BD.total_words.ContainsKey(word.Key))
                {
                    frecuency += 10 + BD.total_words[word.Key];
                }
            }

            //operador de cercania
            if (query.text.Any(e => e.Contains('~')))
            {
                List<List<string>> near = new List<List<string>>();
                List<List<int>> positions = new List<List<int>>();
                Operators.FillNear(near, query);
                if (near.Count > 0)
                {
                    foreach (var item in near)
                    {
                        int n = Operators.GetNear(BD.documents[index].words_Snippet[i..(i + large)], positions, item);
                        if (n == 0)
                        {
                            frecuency = 0;
                        }
                        else
                        {
                            frecuency *= 100 / n;
                        }
                    }
                }
            }


            //operador de aparicion
            for (int j = 0; j < query.text.Length; j++)
            {
                if (query.text[j][0] == '^' && BD.documents[index].words_Snippet[i..(i + large)].All(word => word != query.words_Snippet[j]))
                {
                    frecuency = 0;
                    break;
                }
            }

            //asignar inicio de snippet
            if (max < frecuency)
            {
                max = frecuency;
                position = i;
            }
        }

        //asignar la snippet
        if (position > -1)
        {
            if (position + large > BD.documents[index].text.Length)
            {
                foreach (var item in BD.documents[index].text[(BD.documents[index].text.Length - 100)..(BD.documents[index].text.Length)])
                {
                    BD.documents[index].Snippet += " " + item;
                }
            }
            else
            {
                foreach (var item in BD.documents[index].text[position..(position + large)])
                {
                    BD.documents[index].Snippet += " " + item;
                }
            }
        }
        else
        {
            BD.documents[index].Snippet = "NOT FOUND";
        }

    }
    private static string SetSuggestion(Doc query, Dictionary<string, float> total_words)
    {
        string suggestion = null;
        foreach (var wordQ in query.words_Snippet)
        {
            if (total_words.ContainsKey(wordQ))
            {
                suggestion += " " + wordQ;
            }
            else
            {
                suggestion += " " + Levenshtein(wordQ, total_words);
            }
        }

        return suggestion;
    }
    private static string Levenshtein(string word, Dictionary<string, float> total_words)
    {
        int lev;
        string similar_word = "";
        int min = int.MaxValue;

        foreach (var item in total_words)
        {
            lev = LevenshteinDistance(word, item.Key);

            if (lev == 0)
            {
                return item.Key;
            }

            if (min > lev)
            {
                min = lev;
                similar_word = item.Key;
            }
        }

        return similar_word;
    }
    private static int LevenshteinDistance(string s, string t)
    {
        double porcentaje = 0;

        // d es una tabla con m+1 renglones y n+1 columnas
        int costo = 0;
        int m = s.Length;
        int n = t.Length;
        int[,] d = new int[m + 1, n + 1];

        // Verifica que exista algo que comparar
        if (n == 0) return m;
        if (m == 0) return n;

        // Llena la primera columna y la primera fila.
        for (int i = 0; i <= m; d[i, 0] = i++) ;
        for (int j = 0; j <= n; d[0, j] = j++) ;


        /// recorre la matriz llenando cada unos de los pesos.
        /// i columnas, j renglones
        for (int i = 1; i <= m; i++)
        {
            // recorre para j
            for (int j = 1; j <= n; j++)
            {
                /// si son iguales en posiciones equidistantes el peso es 0
                /// de lo contrario el peso suma a uno.
                costo = (s[i - 1] == t[j - 1]) ? 0 : 1;
                d[i, j] = System.Math.Min(System.Math.Min(d[i - 1, j] + 1,  //Eliminacion
                              d[i, j - 1] + 1),                             //Insercion 
                              d[i - 1, j - 1] + costo);                     //Sustitucion
            }
        }

        /// Calculamos el porcentaje de cambios en la palabra.
        if (s.Length > t.Length)
            porcentaje = ((double)d[m, n] / (double)s.Length);
        else
            porcentaje = ((double)d[m, n] / (double)t.Length);
        return d[m, n];
    }

}
