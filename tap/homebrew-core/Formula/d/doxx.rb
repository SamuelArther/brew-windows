class Doxx < Formula
  desc "Terminal document viewer for .docx files"
  homepage "https://bgreenwell.github.io/doxx/"
  url "https://github.com/bgreenwell/doxx/releases/download/v0.1.4/doxx-x86_64-pc-windows-msvc.zip"
  sha256 "664e6b5138e3c63d67464c5af59b09d6575ed98c984097c43db924d17f898833"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/bgreenwell/doxx/archive/refs/tags/v0.1.4.tar.gz"
  end

  def install
    bin.install "doxx.exe"
    bin.install "generate_test_docs.exe"
  end

  test do
    system bin/"generate_test_docs"
    assert_path_exists testpath/"tests/fixtures/minimal.docx"

    output = shell_output("#{bin}/doxx #{testpath}/tests/fixtures/minimal.docx")
    assert_match <<~EOS, output
      Document: minimal
      Pages: 1
      Words: 26
    EOS
  end
end
