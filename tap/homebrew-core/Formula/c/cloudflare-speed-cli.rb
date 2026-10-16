class CloudflareSpeedCli < Formula
  desc "Cloudflare-based speed test with optional TUI"
  homepage "https://github.com/kavehtehrani/cloudflare-speed-cli"
  url "https://github.com/kavehtehrani/cloudflare-speed-cli/releases/download/v1.0.9/cloudflare-speed-cli-x86_64-pc-windows-msvc.zip"
  sha256 "1f9fdc1e320cc2f03a19536c92bce5b3239b33e8af304f73be813770c8cbdcb2"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/kavehtehrani/cloudflare-speed-cli/archive/refs/tags/v1.0.9.tar.gz"
  end

  def install
    bin.install "cloudflare-speed-cli.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cloudflare-speed-cli --version 2>&1")
  end
end
