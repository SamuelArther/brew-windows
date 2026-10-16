class Scrutineer < Formula
  desc "Security through scrutiny"
  homepage "https://github.com/alpha-omega-security/scrutineer"
  url "https://github.com/alpha-omega-security/scrutineer/releases/download/v2026.10.10.1/scrutineer-2026.10.10.1-windows-amd64.tar.gz"
  sha256 "ed8bdf04a3766bd5dc3b33b74c77b18191cfe277ef978e540c5f1e2a182d1681"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/alpha-omega-security/scrutineer/archive/refs/tags/v2026.10.10.1.tar.gz"
  end

  def install
    bin.install "scrutineer.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/scrutineer version")

    output = shell_output("#{bin}/scrutineer -runtime brew 2>&1", 1)
    assert_match "runtime: must be \\\"docker\\\", \\\"podman\\\", or \\\"apple\\\"", output
  end
end
