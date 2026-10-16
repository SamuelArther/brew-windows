class LeafMarkdownViewer < Formula
  desc "Terminal Markdown previewer with a GUI-like experience"
  homepage "https://leaf.rivolink.mg/"
  url "https://github.com/RivoLink/leaf/releases/download/1.29.1/leaf-windows-x86_64.exe"
  sha256 "f2b801cbadfb82cd752759a6dcccab21ebd2ae8d3bb34bd606f000f5711e71d7"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/RivoLink/leaf/archive/refs/tags/1.29.1.tar.gz"
  end

  conflicts_with "leaf-proxy", because: "both install `leaf` binaries"

  def install
    bin.install "leaf-windows-x86_64.exe" => "leaf.exe"
  end

  test do
    (testpath/"test.md").write "# Hello\n\nThis is a **test**."
    output = shell_output("#{bin}/leaf --inline test.md")
    assert_match "Hello", output
  end
end
