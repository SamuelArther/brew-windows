class Htmltest < Formula
  desc "HTML validator written in Go"
  homepage "https://github.com/wjdp/htmltest"
  url "https://github.com/wjdp/htmltest/releases/download/v0.17.0/htmltest_0.17.0_windows_amd64.zip"
  sha256 "8cb2844bb40664d43f319d832dcb9820027a43be77b4cf74ec31b9fa167edec6"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/wjdp/htmltest/archive/refs/tags/v0.17.0.tar.gz"
  end

  def install
    bin.install "htmltest.exe"
  end

  test do
    (testpath/"test.html").write <<~HTML
      <!DOCTYPE html>
      <html>
        <body>
          <nav>
          </nav>
          <article>
            <p>Some text</p>
          </article>
        </body>
      </html>
    HTML
    assert_match "htmltest started at", shell_output("#{bin}/htmltest test.html")
  end
end
