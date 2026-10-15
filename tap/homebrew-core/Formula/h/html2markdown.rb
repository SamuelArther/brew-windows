class Html2markdown < Formula
  desc "Convert HTML to Markdown"
  homepage "https://html-to-markdown.com"
  url "https://github.com/JohannesKaufmann/html-to-markdown/releases/download/v2.5.2/html-to-markdown_Windows_x86_64.zip"
  sha256 "1edde415802a0e6a3bc6dcbf8bdcb6eddb792b1702b4f60cfa1a18ba07023aeb"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/JohannesKaufmann/html-to-markdown/archive/refs/tags/v2.5.2.tar.gz"
  end

  def install
    bin.install "html2markdown.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/html2markdown --version")

    assert_match "**important**", pipe_output(bin/"html2markdown", "<strong>important</strong>", 0)
  end
end
