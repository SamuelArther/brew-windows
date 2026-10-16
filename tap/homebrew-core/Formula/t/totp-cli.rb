class TotpCli < Formula
  desc "Authy/Google Authenticator like TOTP CLI tool written in Go"
  homepage "https://yitsushi.github.io/totp-cli/"
  url "https://github.com/yitsushi/totp-cli/releases/download/v1.9.2/totp-cli_Windows_x86_64.zip"
  sha256 "878b88a10a350d070965fe77784ba391e7c014ca01c7d31d40dd467face3fa95"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/yitsushi/totp-cli/archive/refs/tags/v1.9.2.tar.gz"
  end

  def install
    bin.install "totp-cli.exe"
  end

  test do
    assert_match "generate", shell_output("#{bin}/totp-cli help")
    assert_match "storage error", pipe_output("#{bin}/totp-cli list 2>&1", "")
  end
end
