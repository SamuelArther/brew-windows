class YamlCpp < Formula
  desc "C++ YAML parser and emitter for YAML 1.2 spec"
  homepage "https://github.com/jbeder/yaml-cpp"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-yaml-cpp-0.9.0-1-any.pkg.tar.zst"
  sha256 "4c4240f5590115ace50e3a39259a51f671dabbdf3138d13d6dc72885f0393a85"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/jbeder/yaml-cpp/archive/refs/tags/yaml-cpp-0.9.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"yaml-cpp/dll.h"
    assert_path_exists lib/"libyaml-cpp.dll.a"
    assert_path_exists bin/"libyaml-cpp.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/yaml-cpp.pc").read
  end
end
