class Qpdf < Formula
  desc "Tools for and transforming and inspecting PDF files"
  homepage "https://qpdf.sourceforge.io/"
  url "https://github.com/qpdf/qpdf/releases/download/v12.4.2/qpdf-12.4.2-msvc64.zip"
  sha256 "db87077e683630c1217e0e8f9a20a9749d952ab676e881c3689187763a5de25d"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/bin/qpdf.exe", "qpdf"
    windows_exe "libexec/bin/fix-qdf.exe", "fix-qdf"
    windows_exe "libexec/bin/zlib-flate.exe", "zlib-flate"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/qpdf --version")
  end
end
