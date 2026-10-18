namespace JobSeek.Normalizator.Services;

public static class EmbeddingModel
{
    public static readonly string Name = "paraphrase-multilingual-MiniLM-L12-v2";
    public static readonly float SimilarityCutoff = 0.57f;

    // Short windows make MiniLM rank ordinary Polish words above 0.84 against "java",
    // while "javascripcie" scores only 0.34 against "javascript".
    // Cosine stays strict. Inflections enter through word ratio on the same windows.
    public static readonly float WindowSimilarityCutoff = 0.90f;
    public static readonly int WindowWordRatioCutoff = 80;
}
