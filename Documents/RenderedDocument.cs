namespace CentralPress.API.Documents;

public sealed record RenderedDocument(byte[] Bytes, string ContentType, string FileName, string? TemplateName = null);
