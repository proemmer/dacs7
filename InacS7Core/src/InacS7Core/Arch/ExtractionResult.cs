using System.Collections.Generic;

namespace InacS7Core.Arch
{
    public class ExtractionResult
    {
        private readonly List<object> _extractedRawMessages;

        public int BytesExtracted { get; private set; }
        public int BytesNeededForFurtherEvaluation { get; private set; }

        public ExtractionResult(int bytesExtracted, int bytesNeededForFurtherEvaluation, List<object> extractedRawMessages)
        {
            BytesExtracted = bytesExtracted;
            BytesNeededForFurtherEvaluation = bytesNeededForFurtherEvaluation;
            _extractedRawMessages = extractedRawMessages;
        }

        public IEnumerable<object> GetExtractedRawMessages()
        {
            return _extractedRawMessages;
        }
    }
}
