class PandocCrossref < Formula
  desc "Pandoc filter for numbering and cross-referencing"
  homepage "https://lierdakil.github.io/pandoc-crossref/"
  url "https://github.com/lierdakil/pandoc-crossref/releases/download/v0.3.26/pandoc-crossref-Windows-X64.7z"
  sha256 "6b14345ef08bc663c89e1114d714987765ceb966596efad6afbcf3331d13a50e"
  license "GPL-2.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/lierdakil/pandoc-crossref/archive/refs/tags/v0.3.26.tar.gz"
  end

  def install
    bin.install "pandoc-crossref.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/pandoc-crossref --version 2>&1")
  end
end
