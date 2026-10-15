class Tomlplusplus < Formula
  desc "Header-only TOML config file parser and serializer for C++17"
  homepage "https://marzer.github.io/tomlplusplus/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tomlplusplus-3.4.0-3-any.pkg.tar.zst"
  sha256 "a52a44a3d65f6024af0d7115ff52a4bc0dce0db0eed16966bdf3173b6319187d"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/marzer/tomlplusplus/archive/refs/tags/v3.4.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"toml++/toml.h"
    assert_path_exists lib/"libtomlplusplus.dll.a"
    assert_path_exists bin/"libtomlplusplus-3.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/tomlplusplus.pc").read
  end
end
