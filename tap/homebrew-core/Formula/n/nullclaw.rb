class Nullclaw < Formula
  desc "Tiny autonomous AI assistant infrastructure written in Zig"
  homepage "https://nullclaw.io"
  url "https://github.com/nullclaw/nullclaw/releases/download/v2026.5.29/nullclaw-windows-x86_64.zip"
  sha256 "1cf762c049ca9f2c8694d0ae3837d481c9e2c8a89b506744bacb68d2f6791db3"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/nullclaw/nullclaw/releases/download/v2026.5.29/nullclaw-source-v2026.5.29.tar.gz"
  end

  def install
    bin.install "nullclaw.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/nullclaw --version")

    output = shell_output("#{bin}/nullclaw status 2>&1")
    assert_match "nullclaw Status", output
  end
end
