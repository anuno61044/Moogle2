namespace MoogleEngine;

public class Doc
{
    public string title { get; set; }
    public string[] text { get; set; }
    public string[] words_Snippet { get; private set; }
    public Dictionary<string, float> TFxIDF { get; set; }
    public float score { get; set; }
    public string Snippet { get; set; }

    //Constructores para documentos
    public Doc(string title, string doc, Dictionary<string, float> total_words)
    {
        //titulo quitandole el ./Content/
        this.title = title.Remove(0, 11);

        //array con las palabras y su signo correspondiente
        text = doc.Split(characters1, StringSplitOptions.RemoveEmptyEntries);

        //array con las palabras sin su signo
        words_Snippet = doc.Split(characters, StringSplitOptions.RemoveEmptyEntries);

        //poner las palabras en minusculas
        for (int i = 0; i < words_Snippet.Length; i++)
        {
            words_Snippet[i] = words_Snippet[i].ToLower();
        }

        TFxIDF = new Dictionary<string, float>();

        //asignar a cada palabra del documento su TF y a esa misma palabra 
        //a nivel de base de datos en cuantos documentos aparece
        fillwords(total_words);

    }

    //constructor para la query
    public Doc(string doc)
    {
        //palanras con su operador correspondiente
        text = Operators.QueryOperators(doc, characters_query);

        //palabras sin los operadores
        words_Snippet = doc.Split(characters, StringSplitOptions.RemoveEmptyEntries);

        //poner las palabras en minusculas
        for (int i = 0; i < words_Snippet.Length; i++)
        {
            words_Snippet[i] = words_Snippet[i].ToLower();
        }

        TFxIDF = new Dictionary<string, float>();
        fillwords();
    }

    private void fillwords(Dictionary<string, float> total_words)
    {
        foreach (var item in words_Snippet)
        {
            if (TFxIDF.ContainsKey(item))
            {
                TFxIDF[item]++;
            }
            else
            {
                TFxIDF.Add(item, 1);

                if (total_words.ContainsKey(item))
                {
                    total_words[item]++;
                }
                else
                {
                    total_words.Add(item, 1);
                }
            }

            

        }
    }
    private void fillwords()
    {
        foreach (var item in words_Snippet)
        {
            if (TFxIDF.ContainsKey(item))
            {
                TFxIDF[item]++;
            }
            else
            {
                TFxIDF.Add(item, 1);
            }

        }
    }


    //cositas privadas de la clase
    private char[] characters1 = { '\n', '\r', '\t', ' ' };
    private char[] characters = { '~', '¿', ' ', '«', '\t', '\r', '\n', '»', '!', '`', '@', '#', '$', '%', '^', '&', '*', '(', ')', '_', '-', '+', '=', '}', ']', '{', '[', ']', '"', ':', ';', '/', '?', '.', '>', ',', '<', '|' };
    private char[] characters_query = { '¿', ' ', '«', '\t', '\n', '»', '`', '@', '#', '$', '%', '&', '(', ')', '_', '-', '+', '=', '}', ']', '{', '[', ']', '"', ':', ';', '/', '?', '.', '>', ',', '<', '|' };

}