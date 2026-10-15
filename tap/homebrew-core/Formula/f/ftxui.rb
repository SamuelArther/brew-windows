class Ftxui < Formula
  desc "C++ Functional Terminal User Interface"
  homepage "https://arthursonzogni.github.io/FTXUI/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-ftxui-7.0.3-1-any.pkg.tar.zst"
  sha256 "8db68970d28215ac1a55627f4e0c3f7a9e45d6897b7b59190c704f3297172253"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/ArthurSonzogni/FTXUI/releases/download/v7.0.3/source.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"ftxui/ftxui.hpp"
    assert_path_exists lib/"libftxui-component.dll.a"
    assert_path_exists bin/"libftxui-component.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/ftxui.pc").read
  end
end
