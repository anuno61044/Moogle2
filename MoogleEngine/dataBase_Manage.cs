namespace MoogleEngine;

public class dataBase_Manage
{
    public Dictionary<string, float> total_words { get; set; }
    public  List<Doc> documents { get; set; }

    public dataBase_Manage(string address)
    {
        //cada palabra de la base de datos se le asocia su IDF
        total_words = new Dictionary<string, float>();
        
        //nombre de los ficheros txt
        string[] name_Docs = Directory.GetFiles(address);
        
        //lista con la class Doc instanciada en cada documento
        documents = new List<Doc>();
        for (int i = 0; i < name_Docs.Length; i++)
        {
            TextReader leer = new StreamReader(name_Docs[i]);
            documents.Add(new Doc(name_Docs[i], leer.ReadToEnd(), total_words));
        }

        
    }
}

