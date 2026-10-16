class Tsuzuri < Formula
  desc "Block-based Markdown notebook for the terminal"
  homepage "https://github.com/jaisuriya-11/tsuzuri"
  url "https://github.com/jaisuriya-11/tsuzuri/releases/download/v0.3.1/tsuzuri-windows.zip"
  sha256 "3f65c281aac7563b83252237c5b21266f98495608b340332701c8c1632f669e5"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/jaisuriya-11/tsuzuri/archive/refs/tags/v0.3.1.tar.gz"
  end

  def install
    bin.install "tsuzuri.exe"
  end

  test do
    assert_match "tsuzuri #{version}", shell_output("#{bin}/tsuzuri --version")

    (testpath/"note.md").write <<~MARKDOWN
      # Homebrew

      A notebook with **bold text** and a list:

      - First item
      - Second item
    MARKDOWN
    system bin/"tsuzuri", "export", "note.md"
    pdf = (testpath/"note.pdf").binread
    assert_match(/\A%PDF-/, pdf)
    assert_match "/Type /Page", pdf
  end
end
