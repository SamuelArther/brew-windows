class Comrak < Formula
  desc "CommonMark + GFM compatible Markdown parser and renderer"
  homepage "https://comrak.ee"
  url "https://github.com/kivikakk/comrak/releases/download/v0.56.0/comrak-0.56.0-x86_64-pc-windows-msvc.exe"
  sha256 "f40ef8f7d68d89b4804c91835c4c5b88e5b00f6981411219ddcb73aa885940e1"
  license "BSD-2-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/kivikakk/comrak/archive/refs/tags/v0.56.0.tar.gz"
  end

  def install
    bin.install "comrak-0.56.0-x86_64-pc-windows-msvc.exe" => "comrak.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/comrak --version")

    (testpath/"test.md").write <<~MARKDOWN
      # Hello, World!

      This is a test of the **comrak** Markdown parser.
    MARKDOWN

    output = shell_output("#{bin}/comrak test.md")
    assert_match "<h1>Hello, World!</h1>", output
    assert_match "<p>This is a test of the <strong>comrak</strong> Markdown parser.</p>", output
  end
end
