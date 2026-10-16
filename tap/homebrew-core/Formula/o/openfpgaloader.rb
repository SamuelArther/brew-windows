class Openfpgaloader < Formula
  desc "Universal utility for programming FPGA"
  homepage "https://trabucayre.github.io/openFPGALoader/"
  url "https://github.com/trabucayre/openFPGALoader/releases/download/v1.1.1/mingw-w64-clang-x86_64-openFPGALoader-ci-1-any.pkg.tar.zst"
  sha256 "818e97adde715e1e4c91373eb162e2803d9bf7a97a1b3ab7d39832397a6cb120"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/trabucayre/openFPGALoader/archive/refs/tags/v1.1.1.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/clang64/bin/openFPGALoader.exe", "openFPGALoader"
  end

  test do
    assert_path_exists bin/"openFPGALoader.exe"
  end
end
