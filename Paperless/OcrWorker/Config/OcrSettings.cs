namespace OcrWorker.Config
{
    public class OcrSettings
    {
        public string Language { get; set; } = "eng";
        public string TempRoot { get; set; } = "/tmp/ocr";
    }
}