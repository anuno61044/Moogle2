using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Linq;
namespace MoogleEngine;

public static class VectorialModel
{
    private static void SetIDF(dataBase_Manage BD)
    {
        foreach (var item in BD.total_words)
        {
            BD.total_words[item.Key] = (float)Math.Log10((BD.documents.Count + 1) / item.Value);
        }
    }
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
    private static float Module(Doc doc)
    {
        float module = 0;

        foreach (var word in doc.TFxIDF)
        {
            module += (float)Math.Pow(word.Value, 2);
        }

        return (float)Math.Sqrt(module);

    }
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

}