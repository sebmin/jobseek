using JobSeek.Normalizator.Services;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;

namespace JobSeek.Normalizator.Services.Providers;

public class EmbedderProvider : IEmbedderProvider
{
    private readonly InferenceSession _session;
    private readonly SentencePieceTokenizer _tokenizer;
    private readonly bool _requiresTokenTypeIds;
    private bool _disposed;

    public EmbedderProvider()
        : this(Resolve("model.onnx"), Resolve("sentencepiece.bpe.model"))
    {
    }

    public EmbedderProvider(string modelPath, string tokenizerPath)
    {
        _session = new InferenceSession(modelPath);
        using var tokenizer = File.OpenRead(tokenizerPath);
        _tokenizer = SentencePieceTokenizer.Create(tokenizer, addBeginningOfSentence: false, addEndOfSentence: false);
        _requiresTokenTypeIds = _session.InputMetadata.ContainsKey("token_type_ids");
    }

    public float[] Embed(string text)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var inputIds = Encode(text);
        var attentionMask = new long[inputIds.Length];
        Array.Fill(attentionMask, 1L);
        int[] dimensions = [1, inputIds.Length];

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", new DenseTensor<long>(inputIds, dimensions)),
            NamedOnnxValue.CreateFromTensor("attention_mask", new DenseTensor<long>(attentionMask, dimensions))
        };

        if (_requiresTokenTypeIds)
        {
            var tokenTypeIds = new long[inputIds.Length];
            inputs.Add(NamedOnnxValue.CreateFromTensor("token_type_ids", new DenseTensor<long>(tokenTypeIds, dimensions)));
        }

        using var outputs = _session.Run(inputs);
        return ToVector(outputs.First().AsTensor<float>(), inputIds.Length);
    }

    public float Similarity(float[] vectorA, float[] vectorB)
    {
        var dotProduct = 0f;
        var normA = 0f;
        var normB = 0f;

        for (var i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }

        if (normA == 0f || normB == 0f)
            return 0f;

        return dotProduct / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _session.Dispose();
        _disposed = true;
    }

    private long[] Encode(string text)
    {
        var pieces = _tokenizer.EncodeToIds(text, addBeginningOfSentence: false, addEndOfSentence: false);
        var ids = new long[pieces.Count + 2];
        ids[0] = 0;
        for (var i = 0; i < pieces.Count; i++)
            ids[i + 1] = pieces[i] == 0 ? 3 : pieces[i] + 1;
        ids[^1] = 2;
        return ids;
    }

    private static float[] ToVector(Tensor<float> tensor, int sequenceLength)
    {
        if (tensor.Rank == 2)
            return Normalize(tensor.ToArray());

        var embeddingDimension = tensor.Dimensions[2];
        var pooled = new float[embeddingDimension];
        for (var dimension = 0; dimension < embeddingDimension; dimension++)
        {
            var sum = 0f;
            for (var token = 0; token < sequenceLength; token++)
                sum += tensor[0, token, dimension];
            pooled[dimension] = sum / sequenceLength;
        }

        return Normalize(pooled);
    }

    private static float[] Normalize(float[] vector)
    {
        var norm = MathF.Sqrt(vector.Sum(value => value * value));
        if (norm == 0f)
            return vector;

        return vector.Select(value => value / norm).ToArray();
    }

    private static string Resolve(string fileName)
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, "Models", EmbeddingModel.Name, fileName);
        if (File.Exists(outputPath))
            return outputPath;

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var projectPath = Path.Combine(directory.FullName, "JobSeek.Normalizator.Services", "Models", EmbeddingModel.Name, fileName);
            if (File.Exists(projectPath))
                return projectPath;

            var modelPath = Path.Combine(directory.FullName, "Models", EmbeddingModel.Name, fileName);
            if (File.Exists(modelPath))
                return modelPath;

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Embedding file '{fileName}' was not found under Models/{EmbeddingModel.Name}.",
            fileName);
    }
}

internal interface IEmbedderProvider : IDisposable
{
    float[] Embed(string inputText);
    float Similarity(float[] a, float[] b);
}
