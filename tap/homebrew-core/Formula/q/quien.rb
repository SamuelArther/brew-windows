class Quien < Formula
  desc "Better WHOIS and domain intelligence toolkit"
  homepage "https://benword.com/quien-a-better-whois-and-domain-intelligence-toolkit"
  url "https://github.com/retlehs/quien/releases/download/v0.12.0/quien_windows_amd64.zip"
  sha256 "8697585fdd1c8e59b82787311ed202749fde7f5253752c417a76be0299ac07ce"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/retlehs/quien/archive/refs/tags/v0.12.0.tar.gz"
  end

  def install
    bin.install "quien.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/quien --version 2>&1")
  end
end
