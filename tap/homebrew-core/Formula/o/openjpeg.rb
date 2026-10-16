class Openjpeg < Formula
  desc "Library for JPEG-2000 image manipulation"
  homepage "https://www.openjpeg.org/"
  url "https://github.com/uclouvain/openjpeg/releases/download/v2.5.4/openjpeg-v2.5.4-windows-x64.zip"
  sha256 "655f6111449da83f5424f76d74873116bc01ce50cc10361d2b0b4667c3e5e8c3"
  license "BSD-2-Clause"
  revision 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/uclouvain/openjpeg/archive/refs/tags/v2.5.4.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/bin/opj_compress.exe", "opj_compress"
    windows_exe "libexec/bin/opj_decompress.exe", "opj_decompress"
    windows_exe "libexec/bin/opj_dump.exe", "opj_dump"
  end

  test do
    assert_path_exists bin/"opj_compress.exe"
  end
end
