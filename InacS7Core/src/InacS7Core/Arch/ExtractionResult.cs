using System.Collections.Generic;

namespace InacS7Core.Arch
{
    public class ExtractionResult
    {
        private readonly IEnumerable<IEnumerable<byte>> _extractedRawMessages;

        public int BytesExtracted { get; private set; }
        public int BytesNeededForFurtherEvaluation { get; private set; }

        public ExtractionResult(int bytesExtracted, int bytesNeededForFurtherEvaluation, IEnumerable<IEnumerable<byte>> extractedRawMessages)
        {
            BytesExtracted = bytesExtracted;
            BytesNeededForFurtherEvaluation = bytesNeededForFurtherEvaluation;
            _extractedRawMessages = extractedRawMessages;
        }

        public IEnumerable<IEnumerable<byte>> GetExtractedRawMessages()
        {
            return _extractedRawMessages;
        }
    }
}
