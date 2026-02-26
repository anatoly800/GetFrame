// Ignore Spelling: Fmpeg Vid

namespace GetFrame.Core.Models;

public enum VideoServiceErrorCode
{
    None = 0,
    FfprobeNotFound = 1,
    FfmpegNotFound = 2,
    FileNotFound = 3,
    InvalidFrameRange = 4,
    FfprobeFailed = 5,
    FfmpegFailed = 6,
    OperationCancelled = 7,
    MetadataRetrievalFailed = 8,
    OperationCanceled = 9
}
