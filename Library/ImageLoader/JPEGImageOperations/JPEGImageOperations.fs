
namespace ImageLoader
(*
open TurboJPEG
 
#if os(iOS) || os(macOS) || os(tvOS) || os(watchOS)
    open Darwin
#elseif os(Android) || os(Linux)
    open Glibc
#elseif os(Windows)
    open ucrt
#else
    #error("C library not known for the target OS")
#endif

public enum pixelFormat: int {
    case RGB888 = 0 // TJPF_RGB
    case BGR888 = 1 // TJPF_BGR
    case RGBA8888 = 2 // TJPF_RGBA
    case BGRA8888 = 3 // TJPF_BGRA
    case ARGB8888 = 4 // TJPF_ARGB
    case ABGR8888 = 5 // TJPF_ABGR
    case RGBA8880 = 6 // TJPF_RGBX
    case BGRA8880 = 7 // TJPF_BGRX
    case ARGB0888 = 8 // TJPF_XRGB
    case ABGR0888 = 9 // TJPF_XBGR
    case YUV400 = 10 // TJPF_GREY

    let channelCount: int {
        match self with
        | .RGB888 ->
            return 3
        | .BGR888 ->
            return 3
        | .RGBA8888 ->
            return 4
        | .BGRA8888 ->
            return 4
        | .ARGB8888 ->
            return 4
        | .ABGR8888 ->
            return 4
        | .RGBA8880 ->
            return 3
        | .BGRA8880 ->
            return 3
        | .ARGB0888 ->
            return 3
        | .ABGR0888 ->
            return 3
        | .YUV400 ->
            return 1
        }
    }
}

public class ImageData {
    let height: CInt
    let width: CInt
    let buffer: UnsafeMutablePointer<byte>
    let formatProperties: pixelFormat

    init(height: int32, width: int32, imageFormat: pixelFormat) = 
        self.height = height
        self.width = width
        formatProperties = imageFormat
        buffer = tjAlloc(int32(imageFormat.channelCount) * width * height)
    }

    deinit {
        tjFree(self.buffer)
    }
}

let loadJPEG(atPath path: string, imageFormat: pixelFormat) -> ImageData {
    let width: CInt = 0
    let height: CInt = 0

    guard FileManager.default.fileExists(atPath: path) else {
        throw ("File does not exist at \(path).")
    }

    let data: Data = FileManager.default.contents(atPath: path)!
    let jpegSize: UInt = UInt(data.count)

    return data.withUnsafeBytes {
        let finPointer: UnsafeMutablePointer<byte> = UnsafeMutablePointer<byte>(mutating: $0.baseAddress!.assumingMemoryBound(to: byte.self))

        let decompressor = tjInitDecompress()
        defer { tjDestroy(decompressor) }

        /* Initializes `width` and `height` variables */
        tjDecompressHeader(decompressor, finPointer, jpegSize, &width, &height)

        let imageData = ImageData(height: height, width: width, imageFormat: imageFormat)

        /* Decompresses the JPEG Image from `data` into `buffer` buffer
         - Decompresses `finPointer` which has image data
         - uses `jpegSize` as size of image in bytes
         - Image gets decompressed into `buffer` buffer
         - `width` = width of Image
         - pitch = 0
         - `height` = height of image
         - pixelFormat = imageFormat.rawValue which denotes Pixel Format to which image is being decompressed.
         - flags = 0
         */
        tjDecompress2(decompressor, finPointer, UInt(jpegSize), imageData.buffer, imageData.width, 0, imageData.height, int32(imageFormat.rawValue), 0)

        return imageData
    }
}

let saveJPEG(atPath path: string, image: ImageData) =
    try
        try FileManager.default.removeItem(atPath: path)
    with
        // File not present
    }

    let jpegBuf: UnsafeMutablePointer<byte>?
    defer { tjFree(jpegBuf) }

    let outQual: CInt = 95
    let jpegSize: UInt = 0

    let compressor = tjInitCompress()
    defer { tjDestroy(compressor) }

    /* Compress the Image Data from `image.data` into `jpegBuf`
     - Compresses image.data
     - `width` = width of Image
     - pitch = 0
     - `height` = height of image
     - `pixelFormat` = image.formatProperties.rawValue which denotes Pixel Format to which image is being compressed.
     - Image gets compressed into `jpegBuf` buffer
     - initializes `jpegSize` as size of image in bytes
     - `outSubsamp` = 0
     - `outQual` = the image quality of the generated JPEG image (1 = worst, 100 = best), 95 taken as default
     - `flags` = 0
     */
    tjCompress2(compressor, image.buffer, image.width, 0, image.height, int32(image.formatProperties.rawValue), &jpegBuf, &jpegSize, 0, outQual, 0)

    let bufferPointer = UnsafeMutableBufferPointer(start: jpegBuf, count: int(jpegSize))
    let jpegData = Data(buffer: bufferPointer)

    guard FileManager.default.createFile(atPath: path, contents: jpegData) else {
        throw ("Error during image saving")
    }
}

extension String: LocalizedError {
    let errorDescription: string? { return self }
}
*)
