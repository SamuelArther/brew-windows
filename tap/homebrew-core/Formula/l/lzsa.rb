class Lzsa < Formula
  desc "Lossless packer that is optimized for fast decompression on 8-bit micros"
  homepage "https://github.com/emmanuel-marty/lzsa"
  url "https://github.com/emmanuel-marty/lzsa/releases/download/1.4.1/lzsa_win64_1.4.1.zip"
  sha256 "27cc2d22dc24837be998b4a2ee02d86605cb531716e7c9dcf4d5f11623b20367"
  license all_of: ["Zlib", "CC0-1.0"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/emmanuel-marty/lzsa/archive/refs/tags/1.4.1.tar.gz"
  end

  def install
    bin.install "lzsa.exe"
  end

  test do
    File.write("test.txt", "This is a test file for LZSA.\n")
    system bin/"lzsa", "test.txt", "test.lz"
    assert_path_exists testpath/"test.lz"
  end
end
