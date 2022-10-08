# Clases con sus Metodos

## Class Moogle

Clase estática y la principal, no consta de ninguna propiedad y posee 8 métodos.

### Query

-   ```cs
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
    ```
    Es el método principal. Efectúa las órdenes y organiza el trabajo de todas las demás las clases encargadas de la búsqueda y los cálculos( *tf*, *idf*, *score*, *cercanía*, *etc* )

### ValidQuery

-   ```cs
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
    ```
    Se ejecuta para si ninguna palabra de la query coincide con alguna de algún documento no efectuar ningún cálculo. Lleva a la página `"Not found"`

### SetSnippets

-   ```cs
    private static void SetSnippets(Doc query, dataBase_Manage BD)
    {
        int large = 100;
        for (int i = 0; i < BD.documents.Count; i++)
        {
            if (BD.documents[i].title == "alfa_romeo_mito.txt")
            {
                Console.WriteLine();
            }
            if (BD.documents[i].score != 0)
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
    ```
    Asigna la snippet que devolverá cada documento según la query. Llama a GetSnippet para obtener la Snippet que asignará

### GetSnippet

-   ```cs
    private static void GetSnippet(Doc query, dataBase_Manage BD, int index, int large)
    {
        int position = -1;
        float max = 0;
        float frecuency;
        position = -1;

        //buscar en todas las posibles snippets la mejor
        for (int i = 0; i + large <= BD.documents[index].words_Snippet.Length; i++)
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
            foreach (var item in BD.documents[index].text[position..(position + large)])
            {
                BD.documents[index].Snippet += " " + item;
            }
        }
        else
        {
            BD.documents[index].Snippet = "NOT FOUND";
        }
    }
    ```
Analiza por documento cuál es el mejor fragmento con respecto a la query para tomarlo como snippet.

### SetSuggestion

-   ```cs
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
    ```
    Analiza si cada palabra de la query no se encuentra en ninguno de los documentos y retorna las palabras más parecidas que sí se encuentren en estos. Llama a Levenshtein

### Levenshtein

-   ```cs
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
    ```
    Establece una comparación entre una palabra de la query y todas las palabras de los documentos (total_words). Llama a LevenshteinDistance para ejecutar el algoritmo de Levenshtein

### LevenshteinDistance

-   ```cs
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
    ```
    Usa el algoritmo de Levenshtein para calcular cuan parecidas son dos palabras. Retorna la cantidad mínima de cambios que hay que hacer ( *eliminar un caracter*, *cambiar un caracter*, *adicionar un caracter* ).

## Class dataBase_Manage

Clase con un constructor y dos propiedades, no tiene métodos.

### Propiedades

- ```cs
    Dictionary<string, float> total_words;
    ```
    En este se encuentran todas las palabras de todos los documentos con su respectivo valor de **IDF**.

- ```cs
    List<Doc> documents;
    ```
    En esta lista aparecen los documentos instanciados como tipo Doc.

### Constructor

- ```cs
    public dataBase_Manage(string address)
    {
        total_words = new Dictionary<string, float>();
        
        string[] name_Docs = Directory.GetFiles(address);
        
        //lista con la class Doc instanciada en cada documento
        documents = new List<Doc>();
        for (int i = 0; i < name_Docs.Length; i++)
        {
            TextReader leer = new StreamReader(name_Docs[i]);
            documents.Add(new Doc(name_Docs[i], leer.ReadToEnd(), total_words));
        }
    }
    ```
    En el constructor se instancian las propiedades y se llena la lista de documentos.

## Class Doc

Cuenta con 2 constructores, uno para instanciar los documentos y otro para la query, 6 propiedades y un metodo con 2 sobrecargas.

### Propiedades

- ```cs
    string title;
    ```
    Título del documento.

- ```cs
    string[] text;
    ```
    El array contiene todas las palabras del documento junto a su signo de puntuación si aparece con este. Es usada para establecer la Snippet en el caso de los documentos, y para obtener los operadores de la query en el caso de la query.

- ```cs
    string[] words_Snippet;
    ```
    El array contiene todas las palabras del documento sin su signo de puntuación en caso que lo tenga en el documento, es usada para encontrar la mejor snippet del documento respecto a la query, para llenar el diccionario TFxIDF y total_words en la clase dataBase_Manage.

- ```cs
    Dictionary<string, float> TFxIDF;
    ```
    En este diccionario aparecen cada palabra del documento (sin repetición) con su valor de **TFxIDF**.

- ```cs
    float score;
    ```
    Valor que indica cuan relevante es el documento segun la búsqueda. Es el resultado de efectuar el cálculo de la `similitud coseno`.

- ```cs
    string Snippet;
    ```
    Fragmento del documento que más se relaciona con la búsqueda del usuario.

- ```cs
    char[] characters1;
    ```
    Utilizado para eliminar saltos de línea, tabulaciones y espacios vacios y añadir palabras a `text`.

- ```cs
    char[] characters;
    ```
    Se utiliza para eliminar cualquier signo de puntuación y añadir las palabras a `words_snippet`.

- ```cs
    char[] characters_query;
    ```
    Es usado en el momento de instanciar la query sin eliminar sus operadores y añadir las palabras a `text`.

### Constructores

- ```cs
    public Doc(string title, string doc, Dictionary<string, float> total_words)
    {
        this.title = title.Remove(0, 11);
        text = doc.Split(characters1, StringSplitOptions.RemoveEmptyEntries);
        words_Snippet = doc.Split(characters, StringSplitOptions.RemoveEmptyEntries);

        //poner las palabras en minusculas
        for (int i = 0; i < words_Snippet.Length; i++)
        {
            words_Snippet[i] = words_Snippet[i].ToLower();
        }

        TFxIDF = new Dictionary<string, float>();
        fillwords(total_words);

    }
    ```
    Se usa para instanciar los documentos y llenar sus propiedades, aunque junto a los demás documentos llena total_words.

- ```cs
    public Doc(string doc)
    {
        text = Operators.QueryOperators(doc, characters_query);
        words_Snippet = doc.Split(characters, StringSplitOptions.RemoveEmptyEntries);

        //poner las palabras en minusculas
        for (int i = 0; i < words_Snippet.Length; i++)
        {
            words_Snippet[i] = words_Snippet[i].ToLower();
        }

        TFxIDF = new Dictionary<string, float>();
        fillwords();
    }
    ```
    Con este constructor se instancia la query y las propiedades excepto title, score y Snippet.

### Fillwords
-   ```cs
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
    ```
    Esta sobrecarga la utiliza el constructor destinado a los documentos, a la vez q llena el diccionario `TFxIDF` también va llenando `total_words`.

-   ```cs
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
    ```
    Esta sobrecarga es usada por la query, únicamente le asigna a cada palabra que adiciona a `TFxIDF` su **TF** para posteriormente multiplicarlo con el **IDF** de esa palabra y asignárselo.

## Class Operators

Esta clase estatica cuenta con 7 métodos y ninguna propiedad.

### QueryOperators

-   ```cs
    public static string[] QueryOperators(string doc, char[] characters_query)
    {
        string[] arr = doc.Split(characters_query, StringSplitOptions.RemoveEmptyEntries);

        //operador de cercania
        List<string> list = new List<string>();

        //hacer la lista
        for (int i = 0; i < arr.Length; i++)
        {
            while (arr[i].Contains('~'))
            {
                if (arr[i][0] == '~')
                {
                    if (list[list.Count - 1].EndsWith('~'))
                    {
                        arr[i] = arr[i].Remove(0, 1);
                    }
                    else
                    {
                        list[list.Count - 1] = list.Last() + "~";
                        arr[i] = arr[i].Remove(0, 1);
                    }
                }
                else
                {
                    int a = arr[i].IndexOf('~');
                    list.Add(arr[i].Substring(0, a + 1));
                    arr[i] = arr[i].Remove(0, a + 1);
                }
            }

            if (arr[i] != "")
            {
                list.Add(arr[i]);
            }

        }
        string[] text = list.ToArray();
        return text;
    }
    ```
    Este método es el encargado de llenar `text` con las palabras de la query y sus respectivos operadores, eliminando cualquier otro signo que tenga.


### wordApear

-   ```cs
    public static bool wordAppear(Doc query, Doc document)
    {
        //Operadores de aparicion
        if (query.text.Any(word => word.Contains('^')) || query.text.Any(word => word.Contains('!')))
        {
            for (int i = 0; i < query.text.Length; i++)
            {
                if (query.text[i][0] == '^' && document.words_Snippet.All(word => word != query.words_Snippet[i]))
                {
                    document.score = 0;
                    return true;
                }

                if (query.text[i][0] == '!' && document.words_Snippet.Any(word => word == query.words_Snippet[i]))
                {
                    document.score = 0;
                    return true;
                }
            }
        }
        return false;
    }
    ```
    Se ocupa de los operadores de existencia pero enfocado en los documentos: para `(^)` devuelve *true* si esa palabra no está en el documento, para `(!)` devuelve *true* si la palabra está en el documento y en ambos casos anteriores anula el `score` del documento, en cualquier otro caso devuelve *false* para calcular el `score` con otro método.


### Importance

-   ```cs
    public static int Importance(Doc query, int i)
    {
        int pot = query.text[i].LastIndexOf('*') + 1;
        if (pot > 10)
        {
            return 10;
        }
        return pot;

    }
    ```
    **Importance** se encarga de si la palabra de la query afectada por alguna cantidad de `(*)` aparezca en algún documento, su **score** aumente.


### NearOperator

-   ```cs
    public static int NearOperator(Doc query, Doc document)
    {
        List<List<string>> near_Words = new List<List<string>>();
        FillNear(near_Words, query);

        //lista con las posiciones de las palabras de la query en el texto
        List<List<int>> words_positions = new List<List<int>>();

        if (near_Words.Count > 0)
        {
            int how_near = 0;
            foreach (var item in near_Words)
            {
                int n = GetNear(document.words_Snippet, words_positions, item);

                if (n == 0)
                {
                    return 0;
                }
                else
                {
                    how_near += n;
                }
            }

            return how_near;
        }
        else
        {
            return -1;
        }

    }
    ```
    Método principal del operador de cercanía, el cual administra el algoritmo de ese operador y devuelve *-1* si el operador no está en la query, *0* si alguna de las palabras cercanas no se encuentran en el documento o `how_near` que representa dicha cercanía entre las palabras.


### FillNear

-   ```cs
    public static void FillNear(List<List<string>> near, Doc query)
    {
        //llenar la lista con las palabras cercanas
        int j = -1;

        for (int i = 0; i < query.text.Length; i++)
        {
            if (query.text[i].Contains('~') && (i == 0 || !(query.text[i - 1].Contains('~'))))
            {
                near.Add(new List<string>());
                j++;
            }
            if (query.text[i].Contains('~') || i != 0 && (query.text[i - 1].Contains('~')))
            {
                near[j].Add(query.words_Snippet[i]);

            }
        }

    }
    ```
    Llena la lista `near` con listas donde cada lista contiene el grupo de palabras que deben estar cerca. 
    
    Ejemplo: sea la query  *"la ~ casa ~ linda de Mickey ~ Mouse"*, entonces `near.Count` = 2, `near[0]` = { *"la"*, *"casa"*, *"linda"* } y `near[1]` = { "*Mickey*", "*Mouse*" }.


### GetNear

-   ```cs
    public static int GetNear(string[] ws, List<List<int>> words_positions, List<string> near)
    {
        int word = 0;
        for (int j = 0; j < near.Count; j++)
        {
            if (ws.Contains(near[j]))
            {
                words_positions.Add(new List<int>());

                for (int i = 0; i < ws.Length; i++)
                {
                    if (ws[i] == near[j])
                    {
                        words_positions[word].Add(i);
                    }
                }
                word++;
            }
            else
            {
                return 0;
            }
        }

        int cercania;
        List<int> elements = new List<int>();
        cercania = Recurs(elements, words_positions, 0);
        return cercania;
    }
    ```
    Este metodo devuelve la menor distancia entre cada grupo de elementos que se relacionen por el operador `(~)`.


### Recurs

-   ```cs
    public static int Recurs(List<int> elements, List<List<int>> positions, int index)
    {
        if (index >= positions.Count)
        {
            return (elements.Max() - elements.Min());
        }

        int cercania = int.MaxValue;
        for (int j = 0; j < positions[index].Count; j++)
        {
            elements.Add(positions[index][j]);
            cercania = Math.Min(cercania, Recurs(elements, positions, index + 1));
            elements.RemoveAt(elements.Count - 1);
        }

        return cercania;
    }
    ```
    **Recurs** analiza de todas las posibles posiciones de las palabras cercanas la que menor diferencia de la mayor posición menos la menor tenga y retorna ese valor de cercanía.


## VectorialModel

La clase esta formada por 6 métodos y no contiene propiedades.

### SetIDF

-   ```cs
    private static void SetIDF(dataBase_Manage BD)
    {
        foreach (var item in BD.total_words)
        {
            BD.total_words[item.Key] = (float)Math.Log10((BD.documents.Count + 1) / item.Value);
        }
    }
    ```
    Calcula el **IDF** a todas las palabras de los documentos en `total_words` y se lo asigna.


### tfxidf_Result

-   ```cs
    public static void tfxidf_Result(dataBase_Manage BD)
    {
        SetIDF(BD);

        foreach (var doc in BD.documents)
        {
            foreach (var word in doc.TFxIDF)
            {
                doc.TFxIDF[word.Key] = word.Value * BD.total_words[word.Key];
            }
        }
    }
    ```
    Esta sobrecarga calcula el **TFxIDF** de cada palabra en los documentos, teniendo previamente calculado el **IDF** y el **TF**



-   ```cs
    public static void tfxidf_Result(dataBase_Manage BD, Doc query)
    {
        foreach (var word in query.TFxIDF)
        {
            if (!BD.total_words.ContainsKey(word.Key))
            {
                query.TFxIDF[word.Key] = 0;
            }
            else
            {
                query.TFxIDF[word.Key] = word.Value * BD.total_words[word.Key];
            }
        }
    }
    ```
    Esta sobrecarga calcula el **TFxIDF** de las palabras de la query.

### SimCos

-   ```cs
    public static void SimCos(dataBase_Manage BD, Doc query)
    {
        foreach (var document in BD.documents)
        {
            if (Operators.wordAppear(query, document))
            {
                continue;
            }
            else
            {
                //operador de cercania
                int near = Operators.NearOperator(query, document);
                if (near == -1)
                {
                    document.score = (float)(DotProduct(query, document) / (Module(query) * Module(document)));
                }
                else
                {
                    if (near == 0)
                    {
                        document.score = 0;
                    }
                    else
                    {
                        document.score = (float)(DotProduct(query, document) / (Module(query) * Module(document))) / near;
                    }
                }

                //operador de importancia
                for (int i = 0; i < query.text.Length; i++)
                {
                    if (query.text[i][0] == '*' && document.TFxIDF.ContainsKey(query.words_Snippet[i]))
                    {
                        document.score *= (float)Math.Pow(2, Operators.Importance(query, i));
                    }

                }

            }
        }
    }
    ```
    Calcula por documento la *similitud coseno* con la query, y le asigna ese resultado al `score` del documento.

### Module

-   ```cs
    private static float Module(Doc doc)
    {
        float module = 0;

        foreach (var word in doc.TFxIDF)
        {
            module += (float)Math.Pow(word.Value, 2);
        }

        return (float)Math.Sqrt(module);
    }
    ```
    Calcula la suma de los cuadrados de los **TFxIDF** de las palabras en el documento y en la query (el método es utilizado por los documentos y la query como un documento), y devuelve su raíz cuadrada.

### DotProduct

-   ```cs
    private static float DotProduct(Doc query, Doc document)
    {
        float dotproduct;
        dotproduct = 0;

        foreach (var word in query.TFxIDF)
        {
            int index;
            if (document.TFxIDF.ContainsKey(word.Key))
            {
                dotproduct += document.TFxIDF[word.Key] * word.Value;
            }
        }

        return dotproduct;
    }
    ```
    Retorna el dotproduct (suma de la multiplicación del **TFxIDF** de cada palabra de la query y esa misma palabra en el documento en caso de que aparezca).
    