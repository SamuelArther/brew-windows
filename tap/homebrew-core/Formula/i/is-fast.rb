class IsFast < Formula
  desc "Check the internet as fast as possible"
  homepage "https://github.com/Magic-JD/is-fast"
  url "https://github.com/Magic-JD/is-fast/releases/download/v0.17.7/is-fast-x86_64-pc-windows-msvc.zip"
  sha256 "0b1a62c533f1e11232ea4a23d3ee9500d7b50505678e975ccc3dae8cddee6942"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Magic-JD/is-fast/archive/refs/tags/v0.17.7.tar.gz"
  end

  def install
    bin.install "is-fast.exe"
  end

  test do
    assert_match "is-fast #{version}", shell_output("#{bin}/is-fast --version")

    (testpath/"test.html").write <<~HTML
      <!DOCTYPE html>
      <html>
        <head><title>Test HTML page</title></head>
        <body>
          <p>Hello Homebrew!</p>
        </body>
      </html>
    HTML

    assert_match "Hello Homebrew!", shell_output("#{bin}/is-fast --piped --file #{testpath}/test.html")
  end
end
