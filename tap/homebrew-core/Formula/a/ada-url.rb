class AdaUrl < Formula
  desc "WHATWG-compliant and fast URL parser written in modern C++"
  homepage "https://ada-url.com"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-ada-url-4.0.0-1-any.pkg.tar.zst"
  sha256 "15a7957f8e1fa12d5f051dce7a34c3c8f9b5894a08390495ec53d595abf86766"
  license any_of: ["Apache-2.0", "MIT"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/ada-url/ada/archive/refs/tags/v4.0.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"ada.h"
    assert_path_exists lib/"libada.dll.a"
    assert_path_exists bin/"libada-4.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/ada.pc").read
  end
end
