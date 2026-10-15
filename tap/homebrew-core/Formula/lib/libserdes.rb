class Libserdes < Formula
  desc "Schema ser/deserializer lib for Avro + Confluent Schema Registry"
  homepage "https://github.com/confluentinc/libserdes"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libserdes-8.0.0-4-any.pkg.tar.zst"
  sha256 "82204dedd7c0ca0dff0ff2e29f195bd5b6f0b8549405c5a6a1a039efe711ae69"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/confluentinc/libserdes.git"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libserdes/serdes.h"
    assert_path_exists lib/"libserdes++.dll.a"
    assert_path_exists bin/"libserdes++-1.dll"
  end
end
