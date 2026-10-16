class VultrCli < Formula
  desc "Command-line tool for Vultr services"
  homepage "https://github.com/vultr/vultr-cli"
  url "https://github.com/vultr/vultr-cli/releases/download/v3.11.0/vultr-cli_v3.11.0_windows_amd64.zip"
  sha256 "1f89f21d20f525b2b0c80630c6edcc691e3dbcb84073bc7ae8902f8a98f4116d"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/vultr/vultr-cli/archive/refs/tags/v3.11.0.tar.gz"
  end

  def install
    bin.install "vultr-cli.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/vultr-cli version 2>&1")
  end
end
