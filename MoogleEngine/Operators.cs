using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Linq;
namespace MoogleEngine;

public class Operators
{
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
    public static int Importance(Doc query, int i)
    {
        int pot = query.text[i].LastIndexOf('*') + 1;
        if (pot > 10)
        {
            return 10;
        }
        return pot;

    }
    public static int NearOperator(Doc query, Doc document)
    {
        //pueden haber varios grupos independientes con palabras cercanas
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
            if (query.text[i].Contains('~') || (i != 0 && (query.text[i - 1].Contains('~'))))
            {
                near[j].Add(query.words_Snippet[i]);

            }
        }

    }
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

}