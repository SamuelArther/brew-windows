class ChaosClient < Formula
  desc "Client to communicate with Chaos DB API"
  homepage "https://chaos.projectdiscovery.io"
  url "https://github.com/projectdiscovery/chaos-client/releases/download/v0.5.2/chaos-client_0.5.2_windows_amd64.zip"
  sha256 "c6bd81092717f9367ec68ccd3e620c1f95a8b02db9778e19d59577da44c08041"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/projectdiscovery/chaos-client/archive/refs/tags/v0.5.2.tar.gz"
  end

  def install
    bin.install "chaos-client.exe" => "chaos.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/chaos --version 2>&1")
  end
end
