class Libyaml < Formula
  desc "YAML Parser"
  homepage "https://github.com/yaml/libyaml"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libyaml-0.2.5-2-any.pkg.tar.zst"
  sha256 "bf4c28a3a92a46db3c809b0789aef731ae9a0c4283db368c7e8624550fed07f3"
  license "MIT"

  livecheck do
    url :homepage
    strategy :github_latest
  end

  # UCRT64 development library; headers/import libraries are installed in the keg include/lib directories.

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the headers, import library, DLL and pkg-config metadata instead.
    assert_match "yaml_parser_initialize", (include/"yaml.h").read
    assert_path_exists lib/"libyaml.dll.a"
    assert_path_exists bin/"libyaml-0-2.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/yaml-0.1.pc").read
  end
end
